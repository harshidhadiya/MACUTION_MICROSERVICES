using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ADMIN.Migrations
{
    /// <inheritdoc />
    public partial class lastadd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "REQUESTS",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "REQUESTS",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                table: "REQUESTS");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "REQUESTS");
        }
    }
}
