using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Verify.Migrations
{
    /// <inheritdoc />
    public partial class anotherlastthings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "VerifiedTime",
                table: "VERIFY_PRODUCTS",
                type: "datetime2",
                nullable: true,
                defaultValueSql: "GETDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETDATE()");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "VERIFY_PRODUCTS",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "isProductVerified",
                table: "VERIFY_PRODUCTS",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "VERIFY_PRODUCTS");

            migrationBuilder.DropColumn(
                name: "isProductVerified",
                table: "VERIFY_PRODUCTS");

            migrationBuilder.AlterColumn<DateTime>(
                name: "VerifiedTime",
                table: "VERIFY_PRODUCTS",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true,
                oldDefaultValueSql: "GETDATE()");
        }
    }
}
