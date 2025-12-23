using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudieAssistenten.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddFlashcardsRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StudyDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    OriginalFilePath = table.Column<string>(type: "TEXT", nullable: true),
                    FileSizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ExtractedText = table.Column<string>(type: "TEXT", nullable: true),
                    TeacherInstructions = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudyDocuments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GeneratedContents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StudyDocumentId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProcessingType = table.Column<int>(type: "INTEGER", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeneratedContents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GeneratedContents_StudyDocuments_StudyDocumentId",
                        column: x => x.StudyDocumentId,
                        principalTable: "StudyDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Flashcards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GeneratedContentId = table.Column<int>(type: "INTEGER", nullable: false),
                    Question = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Answer = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Flashcards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Flashcards_GeneratedContents_GeneratedContentId",
                        column: x => x.GeneratedContentId,
                        principalTable: "GeneratedContents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Flashcards_GeneratedContentId",
                table: "Flashcards",
                column: "GeneratedContentId");

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedContents_StudyDocumentId",
                table: "GeneratedContents",
                column: "StudyDocumentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Flashcards");

            migrationBuilder.DropTable(
                name: "GeneratedContents");

            migrationBuilder.DropTable(
                name: "StudyDocuments");
        }
    }
}
