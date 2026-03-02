using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace User.Migrations
{
    /// <inheritdoc />
    public partial class secondadd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "USERS",
                columns: new[] { "Id", "Address", "CreatedAt", "Email", "HashPassword", "Name", "Phone", "ProfilePicture", "Role" },
                values: new object[] { 1, "123 Main St", new DateTime(2024, 6, 1, 5, 30, 0, 0, DateTimeKind.Local), "harshid.hadiya@gmail.com", "AQAAAAIAAYagAAAAEFT9RFfK4iHUi5Ju7L5g9318ZE/4RtcErhPGnBI8QT/MM0Rtp/4ZoNLdKqcjs1yI8A==", "harshid", "1234567890", null, "SELLER" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "USERS",
                keyColumn: "Id",
                keyValue: 1);
        }
    }
}
