using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ADMIN.Migrations
{
    /// <inheritdoc />
    public partial class thirdadded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "REQUESTS",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestUserId = table.Column<int>(type: "int", nullable: false),
                    VerifierId = table.Column<int>(type: "int", nullable: false),
                    VerifiedByAdmin = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RightToAdd = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValue: new DateTime(2026, 3, 2, 15, 59, 12, 655, DateTimeKind.Utc).AddTicks(2021)),
                    VerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RightsGrantedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_REQUESTS", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_REQUESTS_RequestUserId",
                table: "REQUESTS",
                column: "RequestUserId");

            migrationBuilder.CreateIndex(
                name: "IX_REQUESTS_RightToAdd",
                table: "REQUESTS",
                column: "RightToAdd");

            migrationBuilder.CreateIndex(
                name: "IX_REQUESTS_VerifiedByAdmin",
                table: "REQUESTS",
                column: "VerifiedByAdmin");

            migrationBuilder.CreateIndex(
                name: "IX_REQUESTS_VerifierId",
                table: "REQUESTS",
                column: "VerifierId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "REQUESTS");
        }
    }
}
