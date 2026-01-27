using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BosDAT.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameToCoursetype : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_courses_lesson_types_LessonTypeId",
                table: "courses");

            migrationBuilder.DropTable(
                name: "teacher_lesson_types");

            migrationBuilder.DropTable(
                name: "lesson_types");

            migrationBuilder.RenameColumn(
                name: "LessonTypeId",
                table: "courses",
                newName: "CourseTypeId");

            migrationBuilder.RenameIndex(
                name: "IX_courses_LessonTypeId",
                table: "courses",
                newName: "IX_courses_CourseTypeId");

            migrationBuilder.CreateTable(
                name: "course_types",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InstrumentId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    PriceAdult = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    PriceChild = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    MaxStudents = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_course_types", x => x.Id);
                    table.ForeignKey(
                        name: "FK_course_types_instruments_InstrumentId",
                        column: x => x.InstrumentId,
                        principalTable: "instruments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "teacher_course_types",
                columns: table => new
                {
                    TeacherId = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseTypeId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teacher_course_types", x => new { x.TeacherId, x.CourseTypeId });
                    table.ForeignKey(
                        name: "FK_teacher_course_types_course_types_CourseTypeId",
                        column: x => x.CourseTypeId,
                        principalTable: "course_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_teacher_course_types_teachers_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_course_types_InstrumentId",
                table: "course_types",
                column: "InstrumentId");

            migrationBuilder.CreateIndex(
                name: "IX_teacher_course_types_CourseTypeId",
                table: "teacher_course_types",
                column: "CourseTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_courses_course_types_CourseTypeId",
                table: "courses",
                column: "CourseTypeId",
                principalTable: "course_types",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_courses_course_types_CourseTypeId",
                table: "courses");

            migrationBuilder.DropTable(
                name: "teacher_course_types");

            migrationBuilder.DropTable(
                name: "course_types");

            migrationBuilder.RenameColumn(
                name: "CourseTypeId",
                table: "courses",
                newName: "LessonTypeId");

            migrationBuilder.RenameIndex(
                name: "IX_courses_CourseTypeId",
                table: "courses",
                newName: "IX_courses_LessonTypeId");

            migrationBuilder.CreateTable(
                name: "lesson_types",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InstrumentId = table.Column<int>(type: "integer", nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    MaxStudents = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PriceAdult = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    PriceChild = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lesson_types", x => x.Id);
                    table.ForeignKey(
                        name: "FK_lesson_types_instruments_InstrumentId",
                        column: x => x.InstrumentId,
                        principalTable: "instruments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "teacher_lesson_types",
                columns: table => new
                {
                    TeacherId = table.Column<Guid>(type: "uuid", nullable: false),
                    LessonTypeId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teacher_lesson_types", x => new { x.TeacherId, x.LessonTypeId });
                    table.ForeignKey(
                        name: "FK_teacher_lesson_types_lesson_types_LessonTypeId",
                        column: x => x.LessonTypeId,
                        principalTable: "lesson_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_teacher_lesson_types_teachers_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_lesson_types_InstrumentId",
                table: "lesson_types",
                column: "InstrumentId");

            migrationBuilder.CreateIndex(
                name: "IX_teacher_lesson_types_LessonTypeId",
                table: "teacher_lesson_types",
                column: "LessonTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_courses_lesson_types_LessonTypeId",
                table: "courses",
                column: "LessonTypeId",
                principalTable: "lesson_types",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
