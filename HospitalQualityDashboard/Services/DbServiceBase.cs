// Mục đích: lớp cơ sở đóng gói kết nối SQL, query, execute và helper đọc cột.
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;

namespace HospitalQualityDashboard.Services
{
    public abstract class DbServiceBase
    {
        protected readonly string ConnectionString;

        protected DbServiceBase()
            : this(ConfigurationManager.ConnectionStrings["HospitalQualityConnection"].ConnectionString)
        {
        }

        protected DbServiceBase(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("Connection string is required.", "connectionString");
            }

            ConnectionString = connectionString;
        }

        protected List<T> Query<T>(string sql, Func<SqlDataReader, T> map, params SqlParameter[] parameters)
        {
            var items = new List<T>();
            using (var connection = new SqlConnection(ConnectionString))
            using (var command = new SqlCommand(sql, connection))
            {
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
                command.Parameters.AddRange(parameters);
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    return reader.Read() ? map(reader) : null;
                }
            }
        }

        protected int Execute(string sql, params SqlParameter[] parameters)
        {
            using (var connection = new SqlConnection(ConnectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddRange(parameters);
                connection.Open();
                return command.ExecuteNonQuery();
            }
        }

        protected object Scalar(string sql, params SqlParameter[] parameters)
        {
            using (var connection = new SqlConnection(ConnectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddRange(parameters);
                connection.Open();
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
