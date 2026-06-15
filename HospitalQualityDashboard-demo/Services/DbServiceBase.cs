// Mục đích: lớp cơ sở đóng gói kết nối SQL, query, execute và helper đọc cột.
using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace HospitalQualityDashboardDemo.Services
{
    public abstract class DbServiceBase
    {
        private const int DefaultCommandTimeoutSeconds = 30;

        protected readonly string ConnectionString;
        protected readonly int CommandTimeoutSeconds;

        protected DbServiceBase()
            : this(DatabaseConfiguration.GetConnectionString())
        {
        }

        protected DbServiceBase(string connectionString)
            : this(connectionString, DefaultCommandTimeoutSeconds)
        {
        }

        protected DbServiceBase(string connectionString, int commandTimeoutSeconds)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("Connection string is required.", "connectionString");
            }

            ConnectionString = connectionString;
            CommandTimeoutSeconds = commandTimeoutSeconds > 0 ? commandTimeoutSeconds : DefaultCommandTimeoutSeconds;
        }

        protected List<T> Query<T>(string sql, Func<SqlDataReader, T> map, params SqlParameter[] parameters)
        {
            var items = new List<T>();
            using (var connection = new SqlConnection(ConnectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.CommandTimeout = CommandTimeoutSeconds;
                command.Parameters.AddRange(parameters);
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        items.Add(map(reader));
                    }
                }
            }

            return items;
        }

        protected T QuerySingle<T>(string sql, Func<SqlDataReader, T> map, params SqlParameter[] parameters) where T : class
        {
            using (var connection = new SqlConnection(ConnectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.CommandTimeout = CommandTimeoutSeconds;
                command.Parameters.AddRange(parameters);
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    return reader.Read() ? map(reader) : null;
                }
            }
        }

        protected void ExecuteInTransaction(Action<SqlConnection, SqlTransaction> action)
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        action(connection, transaction);
                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        protected int Execute(string sql, params SqlParameter[] parameters)
        {
            using (var connection = new SqlConnection(ConnectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.CommandTimeout = CommandTimeoutSeconds;
                command.Parameters.AddRange(parameters);
                connection.Open();
                return command.ExecuteNonQuery();
            }
        }

        protected int Execute(SqlConnection connection, SqlTransaction transaction, string sql, params SqlParameter[] parameters)
        {
            using (var command = new SqlCommand(sql, connection, transaction))
            {
                command.CommandTimeout = CommandTimeoutSeconds;
                command.Parameters.AddRange(parameters);
                return command.ExecuteNonQuery();
            }
        }

        protected object Scalar(string sql, params SqlParameter[] parameters)
        {
            using (var connection = new SqlConnection(ConnectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.CommandTimeout = CommandTimeoutSeconds;
                command.Parameters.AddRange(parameters);
                connection.Open();
                return command.ExecuteScalar();
            }
        }

        protected object Scalar(SqlConnection connection, SqlTransaction transaction, string sql, params SqlParameter[] parameters)
        {
            using (var command = new SqlCommand(sql, connection, transaction))
            {
                command.CommandTimeout = CommandTimeoutSeconds;
                command.Parameters.AddRange(parameters);
                return command.ExecuteScalar();
            }
        }

        protected static SqlParameter Param(string name, object value)
        {
            return new SqlParameter(name, value ?? DBNull.Value);
        }

        protected static string String(SqlDataReader reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
        }

        protected static int Int(SqlDataReader reader, string name)
        {
            return reader.GetInt32(reader.GetOrdinal(name));
        }

        protected static int? NullableInt(SqlDataReader reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? (int?)null : reader.GetInt32(ordinal);
        }

        protected static decimal? NullableDecimal(SqlDataReader reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? (decimal?)null : reader.GetDecimal(ordinal);
        }

        protected static DateTime? NullableDateTime(SqlDataReader reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? (DateTime?)null : reader.GetDateTime(ordinal);
        }
    }
}
