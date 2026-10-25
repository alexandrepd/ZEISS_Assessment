using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ZEISS.ProductManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateSequence<int>(
                name: "ProductIds",
                schema: "dbo",
                startValue: 100005L,
                minValue: 100000L,
                maxValue: 999999L);

            migrationBuilder.CreateTable(
                name: "Products",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false, defaultValueSql: "NEXT VALUE FOR [dbo].[ProductIds]"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Stock = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.CheckConstraint("CK_Products_Id", "[Id] BETWEEN 100000 AND 999999");
                    table.CheckConstraint("CK_Products_Name", "LEN(LTRIM(RTRIM([Name]))) > 0");
                    table.CheckConstraint("CK_Products_Price", "[Price] > 0");
                    table.CheckConstraint("CK_Products_Stock", "[Stock] >= 0");
                });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "Products",
                columns: new[] { "Id", "CreatedAtUtc", "Description", "Name", "Price", "Stock", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { 100000, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Precision optical lens", "Optical lens", 125.50m, 10, null },
                    { 100001, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Laboratory microscope", "Microscope", 1500m, 5, null },
                    { 100002, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Microscope camera adapter", "Camera adapter", 89.90m, 20, null },
                    { 100003, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Optical surface cleaning kit", "Lens cleaning kit", 19.99m, 0, null },
                    { 100004, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Microscope calibration slide", "Calibration slide", 45m, 8, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Products",
                schema: "dbo");

            migrationBuilder.DropSequence(
                name: "ProductIds",
                schema: "dbo");
        }
    }
}
