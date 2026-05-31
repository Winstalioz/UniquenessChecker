using System;
using System.Collections.Generic;

namespace UniquenessChecker.Models
{
    public class Role
    {
        public int    RoleId   { get; set; }
        public string RoleName { get; set; } = string.Empty;
    }

    public class User
    {
        public int    UserId       { get; set; }
        public string Login        { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FullName     { get; set; } = string.Empty;
        public string Email        { get; set; } = string.Empty;
        public int    RoleId       { get; set; }
        public string RoleName     { get; set; } = string.Empty;   // из JOIN
        public DateTime CreatedAt  { get; set; }
        public bool   IsActive     { get; set; }

        public bool IsAdmin       => RoleName == "Администратор";
        public bool IsTeacher     => RoleName == "Преподаватель";
        public bool IsStudent     => RoleName == "Студент";
    }

    public class Document
    {
        public int      DocumentId        { get; set; }
        public string   FileName          { get; set; } = string.Empty;
        public int?     FileSizeKb        { get; set; }
        public string   FileExtension     { get; set; } = string.Empty;
        public DateTime UploadDate        { get; set; }
        public int?     UserId            { get; set; }
        public decimal? UniquenessPercent { get; set; }
        public string   Status            { get; set; } = "Pending";

        // Из JOIN с Users (для отображения)
        public string AuthorLogin { get; set; } = string.Empty;
        public string AuthorName  { get; set; } = string.Empty;

        public string UniquenessDisplay =>
            UniquenessPercent.HasValue ? $"{UniquenessPercent:0.00}%" : "—";

        public string StatusDisplay => Status switch
        {
            "Pending"    => "⏳ Ожидает",
            "Processing" => "🔄 Обработка",
            "Done"       => "✅ Готово",
            "Error"      => "❌ Ошибка",
            _            => Status
        };
    }

    public class BorrowingSource
    {
        public int      SourceId         { get; set; }
        public int      DocumentId       { get; set; }
        public string   SourceName       { get; set; } = string.Empty;
        public string?  SourceUrl        { get; set; }
        public decimal? BorrowingPercent { get; set; }
        public DateTime FoundAt          { get; set; }

        public string BorrowingDisplay =>
            BorrowingPercent.HasValue ? $"{BorrowingPercent:0.00}%" : "—";
    }

    public class CheckResult
    {
        public int      ResultId   { get; set; }
        public int      DocumentId { get; set; }
        public DateTime CheckDate  { get; set; }
        public string   Status     { get; set; } = string.Empty;
        public string?  Details    { get; set; }
        public int?     CheckedBy  { get; set; }
    }

    public class Comment
    {
        public int      CommentId   { get; set; }
        public int      DocumentId  { get; set; }
        public int?     UserId      { get; set; }
        public string?  Fragment    { get; set; }
        public string   Body        { get; set; } = string.Empty;
        public DateTime CreatedAt   { get; set; }
        public string   CommentedBy { get; set; } = string.Empty;
    }

    public class DocumentReport
    {
        public Document             Document  { get; set; } = new Document();
        public CheckResult?         Result    { get; set; }
        public List<BorrowingSource> Sources { get; set; } = new List<BorrowingSource>();
        public List<Comment>        Comments  { get; set; } = new List<Comment>();
    }
}
