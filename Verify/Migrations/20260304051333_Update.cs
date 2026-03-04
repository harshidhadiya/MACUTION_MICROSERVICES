using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Verify.Migrations
{
    /// <inheritdoc />
    public partial class Update : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VERIFY_PRODUCTS",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    SellerId = table.Column<int>(type: "int", nullable: false),
                    VerifierId = table.Column<int>(type: "int", nullable: false),
                    VerifiedTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    ProductName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VERIFY_PRODUCTS", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VERIFY_PRODUCTS_ProductId",
                table: "VERIFY_PRODUCTS",
                column: "ProductId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VERIFY_PRODUCTS");
        }
    }
}
