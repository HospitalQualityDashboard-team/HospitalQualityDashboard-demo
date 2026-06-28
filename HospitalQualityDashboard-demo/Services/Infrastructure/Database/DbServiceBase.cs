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

        // Khởi tạo truy cập cơ sở dữ liệu với giá trị mặc định để các luồng xử lý phía sau không gặp trạng thái null ngoài ý muốn.
        protected DbServiceBase()
            : this(DatabaseConfiguration.GetConnectionString())
        {
        }

        // Khởi tạo truy cập cơ sở dữ liệu với giá trị mặc định để các luồng xử lý phía sau không gặp trạng thái null ngoài ý muốn.
        protected DbServiceBase(string connectionString)
            : this(connectionString, DefaultCommandTimeoutSeconds)
        {
        }

        // Khởi tạo truy cập cơ sở dữ liệu với giá trị mặc định để các luồng xử lý phía sau không gặp trạng thái null ngoài ý muốn.
        protected DbServiceBase(string connectionString, int commandTimeoutSeconds)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("Connection string is required.", "connectionString");
            }

            ConnectionString = connectionString;
            CommandTimeoutSeconds = commandTimeoutSeconds > 0 ? commandTimeoutSeconds : DefaultCommandTimeoutSeconds;
        }

        // Chạy SELECT nhiều dòng bằng ADO.NET và map từng dòng sang model nghiệp vụ của service gọi.
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

        // Chạy SELECT một dòng; trả null khi không có dữ liệu để controller/service tự xử lý 404 hoặc empty state.
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

        // Bao nhiều thao tác ghi trong một transaction; rollback toàn bộ nếu bất kỳ bước nào lỗi.
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

        // Thực thi lệnh ghi dữ liệu không cần transaction ngoài, trả về số dòng bị ảnh hưởng.
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

        // Thực thi lệnh ghi dữ liệu trong transaction đang mở để nhiều cập nhật cùng commit/rollback.
        protected int Execute(SqlConnection connection, SqlTransaction transaction, string sql, params SqlParameter[] parameters)
        {
            using (var command = new SqlCommand(sql, connection, transaction))
            {
                command.CommandTimeout = CommandTimeoutSeconds;
                command.Parameters.AddRange(parameters);
                return command.ExecuteNonQuery();
            }
        }

        // Thực thi câu lệnh SQL và trả về dạng kết quả cần thiết.
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

        // Thực thi câu lệnh SQL và trả về dạng kết quả cần thiết.
        protected object Scalar(SqlConnection connection, SqlTransaction transaction, string sql, params SqlParameter[] parameters)
        {
            using (var command = new SqlCommand(sql, connection, transaction))
            {
                command.CommandTimeout = CommandTimeoutSeconds;
                command.Parameters.AddRange(parameters);
                return command.ExecuteScalar();
            }
        }

        // Tạo tham số SQL và chuyển giá trị null sang DBNull an toàn.
        protected static SqlParameter Param(string name, object value)
        {
            return new SqlParameter(name, value ?? DBNull.Value);
        }

        // Đọc giá trị từ nguồn dữ liệu và xử lý trường hợp null an toàn.
        protected static string String(SqlDataReader reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
        }

        // Đọc giá trị từ nguồn dữ liệu và xử lý trường hợp null an toàn.
        protected static int Int(SqlDataReader reader, string name)
        {
            return reader.GetInt32(reader.GetOrdinal(name));
        }

        // Đọc giá trị từ nguồn dữ liệu và xử lý trường hợp null an toàn.
        protected static int? NullableInt(SqlDataReader reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? (int?)null : reader.GetInt32(ordinal);
        }

        // Đọc giá trị từ nguồn dữ liệu và xử lý trường hợp null an toàn.
        protected static decimal? NullableDecimal(SqlDataReader reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? (decimal?)null : reader.GetDecimal(ordinal);
        }

        // Đọc giá trị từ nguồn dữ liệu và xử lý trường hợp null an toàn.
        protected static DateTime? NullableDateTime(SqlDataReader reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? (DateTime?)null : reader.GetDateTime(ordinal);
        }
    }
}
