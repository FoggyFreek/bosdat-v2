using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BosDAT.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLessonTypeSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "settings",
                columns: new[] { "Key", "Description", "Type", "Value" },
                values: new object[,]
                {
                    { "child_discount_percent", "Default percentage discount for child pricing", "decimal", "10" },
                    { "group_max_students", "Default maximum students for group lessons", "int", "6" },
                    { "workshop_max_students", "Default maximum students for workshops", "int", "12" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "Key",
                keyValue: "child_discount_percent");

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "Key",
                keyValue: "group_max_students");

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "Key",
                keyValue: "workshop_max_students");
        }
    }
}
