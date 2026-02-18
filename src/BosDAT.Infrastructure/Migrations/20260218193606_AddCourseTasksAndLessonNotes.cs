using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BosDAT.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseTasksAndLessonNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "course_tasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_course_tasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_course_tasks_courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lesson_notes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LessonId = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lesson_notes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_lesson_notes_lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "note_attachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    StoredFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_note_attachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_note_attachments_lesson_notes_NoteId",
                        column: x => x.NoteId,
                        principalTable: "lesson_notes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_course_tasks_CourseId",
                table: "course_tasks",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_lesson_notes_LessonId",
                table: "lesson_notes",
                column: "LessonId");

            migrationBuilder.CreateIndex(
                name: "IX_note_attachments_NoteId",
                table: "note_attachments",
                column: "NoteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "course_tasks");

            migrationBuilder.DropTable(
                name: "note_attachments");

            migrationBuilder.DropTable(
                name: "lesson_notes");
        }
    }
}
