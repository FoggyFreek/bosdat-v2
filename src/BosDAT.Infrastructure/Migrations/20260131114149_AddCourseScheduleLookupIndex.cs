using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BosDAT.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseScheduleLookupIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "WeekParity",
                table: "courses",
                newName: "week_parity");

            migrationBuilder.AlterColumn<int>(
                name: "week_parity",
                table: "courses",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateIndex(
                name: "ix_courses_schedule_lookup",
                table: "courses",
                columns: new[] { "DayOfWeek", "Frequency", "week_parity" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_courses_schedule_lookup",
                table: "courses");

            migrationBuilder.RenameColumn(
                name: "week_parity",
                table: "courses",
                newName: "WeekParity");

            migrationBuilder.AlterColumn<int>(
                name: "WeekParity",
                table: "courses",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);
        }
    }
}
