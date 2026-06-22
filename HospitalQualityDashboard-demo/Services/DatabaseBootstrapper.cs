// Mục đích: tạo database/schema mẫu khi chạy debug để môi trường dev sẵn sàng.
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Web;
using System.Web.Hosting;

namespace HospitalQualityDashboardDemo.Services
{
    public static class DatabaseBootstrapper
    {
        private const int CommandTimeoutSeconds = 120;

        // Chỉ khởi tạo database khi cờ cấu hình bootstrap được bật rõ ràng.
        public static void BootstrapIfExplicitlyEnabled()
        {
            if (!IsBootstrapEnabled())
            {
                return;
            }

            var databaseConnection = new SqlConnectionStringBuilder(DatabaseConfiguration.GetConnectionString());
            var databaseName = databaseConnection.InitialCatalog;
            if (string.IsNullOrWhiteSpace(databaseName))
            {
                throw new InvalidOperationException("Connection string " + DatabaseConfiguration.ConnectionName + " must include Initial Catalog or Database.");
            }

            var scriptDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "Sql");
            if (string.IsNullOrWhiteSpace(scriptDirectory) || !Directory.Exists(scriptDirectory))
            {
                throw new InvalidOperationException("SQL script directory was not found: ~/App_Data/Sql.");
            }

            if (IsCreateDatabaseEnabled())
            {
                EnsureDatabaseExists(databaseConnection, databaseName);
            }

            RunSchemaScripts(databaseConnection.ConnectionString, scriptDirectory);
        }

        // Xác định dữ liệu có thỏa điều kiện nghiệp vụ của khởi tạo cấu trúc cơ sở dữ liệu hay không.
        private static bool IsBootstrapEnabled()
        {
            var enabled = string.Equals(
                ConfigurationManager.AppSettings["HospitalQualityBootstrapEnabled"],
                "true",
                StringComparison.OrdinalIgnoreCase);
            return enabled;
        }

        // Xác định dữ liệu có thỏa điều kiện nghiệp vụ của khởi tạo cấu trúc cơ sở dữ liệu hay không.
        private static bool IsCreateDatabaseEnabled()
        {
            var enabled = string.Equals(
                ConfigurationManager.AppSettings["HospitalQualityBootstrapCreateDatabase"],
                "true",
                StringComparison.OrdinalIgnoreCase);
            return enabled;
        }

        // Kiểm tra các điều kiện hợp lệ trước khi tiếp tục xử lý khởi tạo cấu trúc cơ sở dữ liệu.
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

        // Kiểm tra bản ghi hoặc đối tượng cần dùng đã tồn tại hay chưa.
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

        // Thực thi quy trình xử lý của khởi tạo cấu trúc cơ sở dữ liệu.
        private static void RunSchemaScripts(string connectionString, string scriptDirectory)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                if (!ObjectExists(connection, "dbo.KhoaPhong"))
                {
                    RunScript(connection, scriptDirectory, "001_CreateSchema.sql");
                    SeedAdminAccount(connection);
                }

                RunOptionalScript(connection, scriptDirectory, "002_PerformanceIndexes.sql");
                RunOptionalScript(connection, scriptDirectory, "004_AddIndicatorWarning.sql");
                RunOptionalScript(connection, scriptDirectory, "005_AddIsHiddenToBaoCao.sql");
            }
        }

        // Tạo tài khoản Admin ban đầu khi hệ thống chưa có dữ liệu quản trị.
        private static void SeedAdminAccount(SqlConnection connection)
        {
            const string sql = @"
IF NOT EXISTS (SELECT 1 FROM dbo.TaiKhoan WHERE TenDangNhap = N'admin')
BEGIN
    INSERT INTO dbo.TaiKhoan (TenDangNhap, MatKhauHash, LoaiTaiKhoan)
    VALUES (N'admin', N'10000:lkAriw5t2BuUCTmEysNbHQ==:BwV/toyAETuBIEzF32EYnWtAlExo4zo54sHjHE4/tds=', 1);
END";
            ExecuteNonQuery(connection, sql);
        }

        // Kiểm tra bản ghi hoặc đối tượng cần dùng đã tồn tại hay chưa.
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

        // Thực thi quy trình xử lý của khởi tạo cấu trúc cơ sở dữ liệu.
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

        // Thực thi quy trình xử lý của khởi tạo cấu trúc cơ sở dữ liệu.
        private static void RunOptionalScript(SqlConnection connection, string scriptDirectory, string fileName)
        {
            var scriptPath = Path.Combine(scriptDirectory, fileName);
            if (!File.Exists(scriptPath))
            {
                return;
            }

            RunScript(connection, scriptDirectory, fileName);
        }

        // Tách nội dung đầu vào thành các phần tử độc lập để xử lý.
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

        // Thực thi quy trình xử lý của khởi tạo cấu trúc cơ sở dữ liệu.
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

        // Thoát và bao giá trị để tạo đầu ra an toàn, đúng định dạng.
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
