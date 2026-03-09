using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ADMIN.Migrations
{
    /// <inheritdoc />
    public partial class second : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "REQUESTS",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GetDate()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2026, 3, 2, 15, 59, 12, 655, DateTimeKind.Utc).AddTicks(2021));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "REQUESTS",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2026, 3, 2, 15, 59, 12, 655, DateTimeKind.Utc).AddTicks(2021),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GetDate()");
        }
    }
}
