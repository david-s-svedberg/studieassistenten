using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudieAssistenten.Server.Migrations
{
    /// <inheritdoc />
    public partial class InitialWithTest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create Tests table
            migrationBuilder.CreateTable(
                name: "Tests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Instructions = table.Column<string>(type: "TEXT", nullable: true),
                    UserId = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tests", x => x.Id);
                });
            
            // Drop TeacherInstructions column from StudyDocuments if it exists
            migrationBuilder.Sql("PRAGMA foreign_keys = OFF;");
            
            migrationBuilder.Sql(@"
                CREATE TABLE StudyDocuments_new (
                    Id INTEGER NOT NULL CONSTRAINT PK_StudyDocuments PRIMARY KEY AUTOINCREMENT,
                    FileName TEXT NOT NULL,
                    OriginalFilePath TEXT NULL,
                    FileSizeBytes INTEGER NOT NULL,
                    ContentType TEXT NOT NULL,
                    UploadedAt TEXT NOT NULL,
                    Status INTEGER NOT NULL,
                    ExtractedText TEXT NULL,
                    TestId INTEGER NULL,
                    CONSTRAINT FK_StudyDocuments_Tests_TestId FOREIGN KEY (TestId) REFERENCES Tests (Id) ON DELETE SET NULL
                );
            ");
            
            migrationBuilder.Sql(@"
                INSERT INTO StudyDocuments_new (Id, FileName, OriginalFilePath, FileSizeBytes, ContentType, UploadedAt, Status, ExtractedText, TestId)
                SELECT Id, FileName, OriginalFilePath, FileSizeBytes, ContentType, UploadedAt, Status, ExtractedText, NULL
                FROM StudyDocuments;
            ");
            
            migrationBuilder.Sql("DROP TABLE StudyDocuments;");
            migrationBuilder.Sql("ALTER TABLE StudyDocuments_new RENAME TO StudyDocuments;");
            
            // Add TestId to GeneratedContents
            migrationBuilder.AddColumn<int>(
                name: "TestId",
                table: "GeneratedContents",
                type: "INTEGER",
                nullable: true);
            
            migrationBuilder.CreateIndex(
                name: "IX_StudyDocuments_TestId",
                table: "StudyDocuments",
                column: "TestId");

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedContents_TestId",
                table: "GeneratedContents",
                column: "TestId");
                
            migrationBuilder.Sql("PRAGMA foreign_keys = ON;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove TestId from GeneratedContents
            migrationBuilder.DropColumn(
                name: "TestId",
                table: "GeneratedContents");
            
            // Recreate StudyDocuments with TeacherInstructions
            migrationBuilder.Sql("PRAGMA foreign_keys = OFF;");
            
            migrationBuilder.Sql(@"
                CREATE TABLE StudyDocuments_new (
                    Id INTEGER NOT NULL CONSTRAINT PK_StudyDocuments PRIMARY KEY AUTOINCREMENT,
                    FileName TEXT NOT NULL,
                    OriginalFilePath TEXT NULL,
                    FileSizeBytes INTEGER NOT NULL,
                    ContentType TEXT NOT NULL,
                    UploadedAt TEXT NOT NULL,
                    Status INTEGER NOT NULL,
                    ExtractedText TEXT NULL,
                    TeacherInstructions TEXT NULL
                );
            ");
            
            migrationBuilder.Sql(@"
                INSERT INTO StudyDocuments_new (Id, FileName, OriginalFilePath, FileSizeBytes, ContentType, UploadedAt, Status, ExtractedText)
                SELECT Id, FileName, OriginalFilePath, FileSizeBytes, ContentType, UploadedAt, Status, ExtractedText
                FROM StudyDocuments;
            ");
            
            migrationBuilder.Sql("DROP TABLE StudyDocuments;");
            migrationBuilder.Sql("ALTER TABLE StudyDocuments_new RENAME TO StudyDocuments;");
            
            migrationBuilder.Sql("PRAGMA foreign_keys = ON;");
            
            // Drop Tests table
            migrationBuilder.DropTable(
                name: "Tests");
        }
    }
}
