using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudieAssistenten.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddTestSharing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TestShares",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TestId = table.Column<int>(type: "INTEGER", nullable: false),
                    OwnerId = table.Column<string>(type: "TEXT", nullable: false),
                    SharedWithUserId = table.Column<string>(type: "TEXT", nullable: false),
                    SharedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Permission = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestShares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestShares_AspNetUsers_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TestShares_AspNetUsers_SharedWithUserId",
                        column: x => x.SharedWithUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TestShares_Tests_TestId",
                        column: x => x.TestId,
                        principalTable: "Tests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TestShares_OwnerId",
                table: "TestShares",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_TestShares_SharedWithUserId",
                table: "TestShares",
                column: "SharedWithUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TestShares_TestId_SharedWithUserId",
                table: "TestShares",
                columns: new[] { "TestId", "SharedWithUserId" },
                unique: true,
                filter: "RevokedAt IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TestShares");
        }
    }
}
