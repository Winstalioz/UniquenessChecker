using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using UniquenessChecker.Models;

namespace UniquenessChecker.Data
{
    /// <summary>
    /// Репозиторий для работы с БД через ADO.NET.
    /// Все запросы — параметризованные (защита от SQL-инъекций).
    /// </summary>
    public class DatabaseRepository
    {
        private readonly string _connStr;

        public DatabaseRepository()
        {
            _connStr = ConfigurationManager.ConnectionStrings["UniquenessDb"].ConnectionString;
        }

        private SqlConnection OpenConnection()
        {
            var conn = new SqlConnection(_connStr);
            conn.Open();
            return conn;
        }

        // =====================================================
        // ПОЛЬЗОВАТЕЛИ
        // =====================================================

        /// <summary>Аутентификация через хранимую процедуру sp_AuthUser</summary>
        public User? Authenticate(string login, string passwordHash)
        {
            using var conn = OpenConnection();
            using var cmd  = new SqlCommand("sp_AuthUser", conn)
                             { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@login",         login);
            cmd.Parameters.AddWithValue("@password_hash", passwordHash);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new User
                {
                    UserId   = reader.GetInt32(0),
                    Login    = reader.GetString(1),
                    FullName = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    Email    = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    RoleName = reader.GetString(4)
                };
            }
            return null;
        }

        public List<User> GetAllUsers()
        {
            var list = new List<User>();
            using var conn = OpenConnection();
            const string sql = @"
                SELECT u.user_id, u.login, u.full_name, u.email,
                       r.role_name, u.created_at, u.is_active
                FROM Users u
                JOIN Roles r ON u.role_id = r.role_id
                ORDER BY u.user_id";
            using var cmd    = new SqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new User
                {
                    UserId    = reader.GetInt32(0),
                    Login     = reader.GetString(1),
                    FullName  = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    Email     = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    RoleName  = reader.GetString(4),
                    CreatedAt = reader.GetDateTime(5),
                    IsActive  = reader.GetBoolean(6)
                });
            }
            return list;
        }

        public void AddUser(string login, string passwordHash, string fullName, string email, int roleId)
        {
            using var conn = OpenConnection();
            using var cmd  = new SqlCommand("sp_AddUser", conn)
                             { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@login",         login);
            cmd.Parameters.AddWithValue("@password_hash", passwordHash);
            cmd.Parameters.AddWithValue("@full_name",     (object?)fullName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@email",         (object?)email    ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@role_id",       roleId);
            cmd.ExecuteNonQuery();
        }

        public void DeleteUser(int userId)
        {
            using var conn = OpenConnection();
            using var cmd  = new SqlCommand(
                "UPDATE Users SET is_active=0 WHERE user_id=@id", conn);
            cmd.Parameters.AddWithValue("@id", userId);
            cmd.ExecuteNonQuery();
        }

        public List<Role> GetRoles()
        {
            var list = new List<Role>();
            using var conn   = OpenConnection();
            using var cmd    = new SqlCommand("SELECT role_id, role_name FROM Roles ORDER BY role_id", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(new Role { RoleId = reader.GetInt32(0), RoleName = reader.GetString(1) });
            return list;
        }

        // =====================================================
        // ДОКУМЕНТЫ
        // =====================================================

        public List<Document> GetAllDocuments()
        {
            var list = new List<Document>();
            using var conn = OpenConnection();
            const string sql = @"
                SELECT d.document_id, d.file_name, d.file_extension,
                       d.file_size_kb, d.upload_date,
                       d.uniqueness_percent, d.status,
                       u.login, u.full_name
                FROM Documents d
                LEFT JOIN Users u ON d.user_id = u.user_id
                ORDER BY d.upload_date DESC";
            using var cmd    = new SqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new Document
                {
                    DocumentId        = reader.GetInt32(0),
                    FileName          = reader.GetString(1),
                    FileExtension     = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    FileSizeKb        = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3),
                    UploadDate        = reader.GetDateTime(4),
                    UniquenessPercent = reader.IsDBNull(5) ? (decimal?)null : reader.GetDecimal(5),
                    Status            = reader.GetString(6),
                    AuthorLogin       = reader.IsDBNull(7) ? "" : reader.GetString(7),
                    AuthorName        = reader.IsDBNull(8) ? "" : reader.GetString(8)
                });
            }
            return list;
        }

        public List<Document> GetDocumentsByUser(int userId)
        {
            var list = new List<Document>();
            using var conn = OpenConnection();
            const string sql = @"
                SELECT document_id, file_name, file_extension,
                       file_size_kb, upload_date,
                       uniqueness_percent, status
                FROM Documents
                WHERE user_id = @uid
                ORDER BY upload_date DESC";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@uid", userId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new Document
                {
                    DocumentId        = reader.GetInt32(0),
                    FileName          = reader.GetString(1),
                    FileExtension     = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    FileSizeKb        = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3),
                    UploadDate        = reader.GetDateTime(4),
                    UniquenessPercent = reader.IsDBNull(5) ? (decimal?)null : reader.GetDecimal(5),
                    Status            = reader.GetString(6),
                    UserId            = userId
                });
            }
            return list;
        }

        public int AddDocument(string fileName, int? sizeKb, string ext, int userId)
        {
            using var conn = OpenConnection();
            const string sql = @"
                INSERT INTO Documents (file_name, file_size_kb, file_extension, user_id)
                OUTPUT INSERTED.document_id
                VALUES (@fn, @sz, @ext, @uid)";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@fn",  fileName);
            cmd.Parameters.AddWithValue("@sz",  (object?)sizeKb ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ext", ext);
            cmd.Parameters.AddWithValue("@uid", userId);
            return (int)cmd.ExecuteScalar();
        }

        public void DeleteDocument(int documentId)
        {
            using var conn = OpenConnection();
            using var cmd  = new SqlCommand(
                "DELETE FROM Documents WHERE document_id=@id", conn);
            cmd.Parameters.AddWithValue("@id", documentId);
            cmd.ExecuteNonQuery();
        }

        // =====================================================
        // ОТЧЁТ
        // =====================================================

        public DocumentReport GetDocumentReport(int documentId)
        {
            var report = new DocumentReport();
            using var conn = OpenConnection();

            // Используем sp_GetDocumentReport — возвращает 3 result set
            using var cmd = new SqlCommand("sp_GetDocumentReport", conn)
                            { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@document_id", documentId);

            using var reader = cmd.ExecuteReader();

            // Result set 1: документ
            if (reader.Read())
            {
                report.Document = new Document
                {
                    DocumentId        = reader.GetInt32(0),
                    FileName          = reader.GetString(1),
                    FileExtension     = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    FileSizeKb        = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3),
                    UploadDate        = reader.GetDateTime(4),
                    UniquenessPercent = reader.IsDBNull(5) ? (decimal?)null : reader.GetDecimal(5),
                    Status            = reader.GetString(6),
                    AuthorName        = reader.IsDBNull(7) ? "" : reader.GetString(7),
                    AuthorLogin       = reader.IsDBNull(8) ? "" : reader.GetString(8)
                };
                if (!reader.IsDBNull(9))
                    report.Result = new CheckResult
                    {
                        CheckDate = reader.GetDateTime(9),
                        Details   = reader.IsDBNull(10) ? null : reader.GetString(10)
                    };
            }

            // Result set 2: источники
            reader.NextResult();
            while (reader.Read())
            {
                report.Sources.Add(new BorrowingSource
                {
                    SourceId         = reader.GetInt32(0),
                    SourceName       = reader.GetString(1),
                    SourceUrl        = reader.IsDBNull(2) ? (string?)null : reader.GetString(2),
                    BorrowingPercent = reader.IsDBNull(3) ? (decimal?)null : reader.GetDecimal(3),
                    FoundAt          = reader.GetDateTime(4)
                });
            }

            // Result set 3: комментарии
            reader.NextResult();
            while (reader.Read())
            {
                report.Comments.Add(new Comment
                {
                    CommentId   = reader.GetInt32(0),
                    Fragment    = reader.IsDBNull(1) ? null : reader.GetString(1),
                    Body        = reader.GetString(2),
                    CreatedAt   = reader.GetDateTime(3),
                    CommentedBy = reader.IsDBNull(4) ? "" : reader.GetString(4)
                });
            }

            return report;
        }

        public void SaveCheckResult(int documentId, decimal uniqueness, string details, int? checkedBy)
        {
            using var conn = OpenConnection();
            using var cmd  = new SqlCommand("sp_SaveCheckResult", conn)
                             { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@document_id",        documentId);
            cmd.Parameters.AddWithValue("@uniqueness_percent", uniqueness);
            cmd.Parameters.AddWithValue("@details",            (object?)details   ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@checked_by",         (object?)checkedBy ?? DBNull.Value);
            cmd.ExecuteNonQuery();
        }

        public void AddBorrowingSource(int documentId, string sourceName, string? sourceUrl, decimal percent)
        {
            using var conn = OpenConnection();
            const string sql = @"
                INSERT INTO BorrowingSources (document_id, source_name, source_url, borrowing_percent)
                VALUES (@did, @sn, @url, @pct)";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@did", documentId);
            cmd.Parameters.AddWithValue("@sn",  sourceName);
            cmd.Parameters.AddWithValue("@url", (object?)sourceUrl ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@pct", percent);
            cmd.ExecuteNonQuery();
        }

        // =====================================================
        // SQL-КОНСОЛЬ (только для администратора)
        // =====================================================

        public DataTable ExecuteRawQuery(string sql)
        {
            using var conn = OpenConnection();
            using var cmd  = new SqlCommand(sql, conn)
                             { CommandTimeout = 30 };
            using var adapter = new SqlDataAdapter(cmd);
            var dt = new DataTable();
            adapter.Fill(dt);
            return dt;
        }

        // =====================================================
        // СТАТИСТИКА
        // =====================================================

        public DataTable GetUserStats()
        {
            using var conn    = OpenConnection();
            using var cmd     = new SqlCommand("SELECT * FROM vw_UserStats ORDER BY total_docs DESC", conn);
            using var adapter = new SqlDataAdapter(cmd);
            var dt = new DataTable();
            adapter.Fill(dt);
            return dt;
        }

        public DataTable GetUniquenessSummary()
        {
            using var conn    = OpenConnection();
            using var cmd     = new SqlCommand("SELECT * FROM vw_UniquenessSummary", conn);
            using var adapter = new SqlDataAdapter(cmd);
            var dt = new DataTable();
            adapter.Fill(dt);
            return dt;
        }

        public void AddComment(int documentId, int userId, string? fragment, string body)
        {
            using var conn = OpenConnection();
            const string sql = @"
                INSERT INTO Comments (document_id, user_id, fragment, body)
                VALUES (@did, @uid, @frag, @body)";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@did",  documentId);
            cmd.Parameters.AddWithValue("@uid",  userId);
            cmd.Parameters.AddWithValue("@frag", (object?)fragment ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@body", body);
            cmd.ExecuteNonQuery();
        }
    }
}
