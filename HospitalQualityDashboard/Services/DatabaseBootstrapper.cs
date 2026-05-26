using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Hosting;

namespace HospitalQualityDashboard.Services
{
    public static class DatabaseBootstrapper
    {
        private const string ConnectionName = "HospitalQualityConnection";
        private const int CommandTimeoutSeconds = 120;

        public static void BootstrapIfDebug()
        {
            if (!IsDebugMode())
            {
                return;
            }

            var connectionSettings = ConfigurationManager.ConnectionStrings[ConnectionName];
            if (connectionSettings == null || string.IsNullOrWhiteSpace(connectionSettings.ConnectionString))
            {
                throw new InvalidOperationException("Missing connection string: " + ConnectionName + ".");
            }

            var databaseConnection = new SqlConnectionStringBuilder(connectionSettings.ConnectionString);
            var databaseName = databaseConnection.InitialCatalog;
            if (string.IsNullOrWhiteSpace(databaseName))
            {
                throw new InvalidOperationException("Connection string " + ConnectionName + " must include Initial Catalog or Database.");
            }

            var scriptDirectory = HostingEnvironment.MapPath("~/App_Data/Sql");
            if (string.IsNullOrWhiteSpace(scriptDirectory) || !Directory.Exists(scriptDirectory))
            {
                throw new InvalidOperationException("SQL script directory was not found: ~/App_Data/Sql.");
            }

            EnsureDatabaseExists(databaseConnection, databaseName);
            RunSchemaScripts(databaseConnection.ConnectionString, scriptDirectory);
        }

        private static bool IsDebugMode()
        {
            return HttpContext.Current != null && HttpContext.Current.IsDebuggingEnabled;
        }

        private static void EnsureDatabaseExists(SqlConnectionStringBuilder databaseConnection, string databaseName)
        {
            var masterConnection = new SqlConnectionStringBuilder(databaseConnection.ConnectionString)
            {
                InitialCatalog = "master"
            };

            try
            {
                using (var connection = new SqlConnection(masterConnection.ConnectionString))
                {
                    connection.Open();
                    if (!DatabaseExists(connection, databaseName))
                    {
                        ExecuteNonQuery(connection, "CREATE DATABASE " + QuoteIdentifier(databaseName));
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to create or verify database '" + databaseName + "'. Check SQL Server name, login permissions, and whether the server is running.", ex);
            }
        }

        private static bool DatabaseExists(SqlConnection connection, string databaseName)
        {
            using (var command = new SqlCommand("SELECT DB_ID(@DatabaseName)", connection))
            {
                command.CommandTimeout = CommandTimeoutSeconds;
                command.Parameters.AddWithValue("@DatabaseName", databaseName);
                var result = command.ExecuteScalar();
                return result != DBNull.Value && result != null;
            }
        }

        private static void RunSchemaScripts(string connectionString, string scriptDirectory)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                if (!ObjectExists(connection, "dbo.KhoaPhong"))
                {
                    RunScript(connection, scriptDirectory, "001_CreateSchema.sql");
                }

                RunScript(connection, scriptDirectory, "002_SeedAdmin.sql");
                RunScript(connection, scriptDirectory, "003_AddIndicatorFrequencies.sql");
                RunScript(connection, scriptDirectory, "004_AddAssignmentUniqueConstraint.sql");

                if (ShouldRunApprovalScript(connection))
                {
                    RunScript(connection, scriptDirectory, "005_AddApprovalAndRejection.sql");
                }

                RunScript(connection, scriptDirectory, "006_AddNotificationAutomationLog.sql");
            }
        }

        private static bool ShouldRunApprovalScript(SqlConnection connection)
        {
            if (!ObjectExists(connection, "dbo.BaoCao"))
            {
                return false;
            }

            var hasFeedbackColumn = Convert.ToInt32(ExecuteScalar(connection, "SELECT CASE WHEN COL_LENGTH('dbo.BaoCao', 'YKienPhanHoi') IS NULL THEN 0 ELSE 1 END")) == 1;
            var constraintDefinition = ExecuteScalar(connection, @"
SELECT definition
FROM sys.check_constraints
WHERE name = N'CK_BaoCao_TrangThai'
  AND parent_object_id = OBJECT_ID(N'dbo.BaoCao')") as string;

            var hasUpdatedStatusConstraint = !string.IsNullOrWhiteSpace(constraintDefinition)
                && Regex.IsMatch(constraintDefinition, @"\b6\b");

            return !hasFeedbackColumn || !hasUpdatedStatusConstraint;
        }

        private static bool ObjectExists(SqlConnection connection, string objectName)
        {
            using (var command = new SqlCommand("SELECT OBJECT_ID(@ObjectName, N'U')", connection))
            {
                command.CommandTimeout = CommandTimeoutSeconds;
                command.Parameters.AddWithValue("@ObjectName", objectName);
                var result = command.ExecuteScalar();
                return result != DBNull.Value && result != null;
            }
        }

        private static void RunScript(SqlConnection connection, string scriptDirectory, string fileName)
        {
            var scriptPath = Path.Combine(scriptDirectory, fileName);
            if (!File.Exists(scriptPath))
            {
                throw new InvalidOperationException("Required SQL script was not found: " + scriptPath);
            }

            try
            {
                var script = File.ReadAllText(scriptPath, Encoding.UTF8);
                foreach (var batch in SplitSqlBatches(script))
                {
                    ExecuteNonQuery(connection, batch);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to execute SQL script: " + fileName + ".", ex);
            }
        }

        private static IEnumerable<string> SplitSqlBatches(string script)
        {
            var batch = new StringBuilder();
            using (var reader = new StringReader(script))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (string.Equals(line.Trim(), "GO", StringComparison.OrdinalIgnoreCase))
                    {
                        if (batch.Length > 0)
                        {
                            yield return batch.ToString();
                            batch.Clear();
                        }
                    }
                    else
                    {
                        batch.AppendLine(line);
                    }
                }
            }

            if (batch.Length > 0)
            {
                yield return batch.ToString();
            }
        }

        private static object ExecuteScalar(SqlConnection connection, string sql)
        {
            using (var command = new SqlCommand(sql, connection))
            {
                command.CommandTimeout = CommandTimeoutSeconds;
                return command.ExecuteScalar();
            }
        }

        private static void ExecuteNonQuery(SqlConnection connection, string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                return;
            }

            using (var command = new SqlCommand(sql, connection))
            {
                command.CommandTimeout = CommandTimeoutSeconds;
                command.ExecuteNonQuery();
            }
        }

        private static string QuoteIdentifier(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier) || identifier.IndexOf('\0') >= 0)
            {
                throw new InvalidOperationException("Invalid SQL Server database name.");
            }

            return "[" + identifier.Replace("]", "]]") + "]";
        }
    }
}
