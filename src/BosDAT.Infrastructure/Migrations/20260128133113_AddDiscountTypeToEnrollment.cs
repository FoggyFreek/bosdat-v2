using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BosDAT.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscountTypeToEnrollment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DiscountType",
                table: "enrollments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(@"
                INSERT INTO settings (""Key"", ""Description"", ""Type"", ""Value"")
                VALUES ('course_discount_percent', 'Discount percentage for students with multiple courses', 'decimal', '10')
                ON CONFLICT (""Key"") DO NOTHING;

                INSERT INTO settings (""Key"", ""Description"", ""Type"", ""Value"")
                VALUES ('family_discount_percent', 'Discount percentage for family members', 'decimal', '10')
                ON CONFLICT (""Key"") DO NOTHING;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "Key",
                keyValue: "course_discount_percent");

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "Key",
                keyValue: "family_discount_percent");

            migrationBuilder.DropColumn(
                name: "DiscountType",
                table: "enrollments");
        }
    }
}
