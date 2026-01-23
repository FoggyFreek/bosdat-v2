using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BosDAT.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCourseTypeIntId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Drop foreign key constraints
            migrationBuilder.DropForeignKey(
                name: "FK_courses_course_types_CourseTypeId",
                table: "courses");

            migrationBuilder.DropForeignKey(
                name: "FK_teacher_course_types_course_types_CourseTypeId",
                table: "teacher_course_types");

            // Step 2: Drop composite primary key on teacher_course_types (includes CourseTypeId)
            migrationBuilder.DropPrimaryKey(
                name: "PK_teacher_course_types",
                table: "teacher_course_types");

            // Step 3: Drop index on teacher_course_types.CourseTypeId
            migrationBuilder.DropIndex(
                name: "IX_teacher_course_types_CourseTypeId",
                table: "teacher_course_types");

            // Step 4: Drop primary key on course_types
            migrationBuilder.DropPrimaryKey(
                name: "PK_course_types",
                table: "course_types");

            // Step 3: Add new UUID columns
            migrationBuilder.AddColumn<Guid>(
                name: "NewId",
                table: "course_types",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<Guid>(
                name: "NewCourseTypeId",
                table: "courses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "NewCourseTypeId",
                table: "teacher_course_types",
                type: "uuid",
                nullable: true);

            // Step 4: Map old INT IDs to new UUIDs
            migrationBuilder.Sql(@"
                UPDATE courses c
                SET ""NewCourseTypeId"" = ct.""NewId""
                FROM course_types ct
                WHERE c.""CourseTypeId"" = ct.""Id"";
            ");

            migrationBuilder.Sql(@"
                UPDATE teacher_course_types tct
                SET ""NewCourseTypeId"" = ct.""NewId""
                FROM course_types ct
                WHERE tct.""CourseTypeId"" = ct.""Id"";
            ");

            // Step 5: Drop old INT columns
            migrationBuilder.DropColumn(
                name: "CourseTypeId",
                table: "courses");

            migrationBuilder.DropColumn(
                name: "CourseTypeId",
                table: "teacher_course_types");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "course_types");

            // Step 6: Rename new columns to original names
            migrationBuilder.RenameColumn(
                name: "NewId",
                table: "course_types",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "NewCourseTypeId",
                table: "courses",
                newName: "CourseTypeId");

            migrationBuilder.RenameColumn(
                name: "NewCourseTypeId",
                table: "teacher_course_types",
                newName: "CourseTypeId");

            // Step 7: Make CourseTypeId non-nullable after data migration
            migrationBuilder.AlterColumn<Guid>(
                name: "CourseTypeId",
                table: "courses",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CourseTypeId",
                table: "teacher_course_types",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            // Step 8: Recreate primary key
            migrationBuilder.AddPrimaryKey(
                name: "PK_course_types",
                table: "course_types",
                column: "Id");

            // Step 9: Recreate foreign key constraints
            migrationBuilder.AddForeignKey(
                name: "FK_courses_course_types_CourseTypeId",
                table: "courses",
                column: "CourseTypeId",
                principalTable: "course_types",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_teacher_course_types_course_types_CourseTypeId",
                table: "teacher_course_types",
                column: "CourseTypeId",
                principalTable: "course_types",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            // Step 10: Recreate composite primary key on teacher_course_types
            migrationBuilder.AddPrimaryKey(
                name: "PK_teacher_course_types",
                table: "teacher_course_types",
                columns: new[] { "TeacherId", "CourseTypeId" });

            // Step 11: Recreate index on teacher_course_types.CourseTypeId
            migrationBuilder.CreateIndex(
                name: "IX_teacher_course_types_CourseTypeId",
                table: "teacher_course_types",
                column: "CourseTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Note: Down migration loses the original INT IDs
            // This is a one-way migration - rolling back will generate new sequential IDs

            migrationBuilder.DropIndex(
                name: "IX_teacher_course_types_CourseTypeId",
                table: "teacher_course_types");

            migrationBuilder.DropPrimaryKey(
                name: "PK_teacher_course_types",
                table: "teacher_course_types");

            migrationBuilder.DropForeignKey(
                name: "FK_courses_course_types_CourseTypeId",
                table: "courses");

            migrationBuilder.DropForeignKey(
                name: "FK_teacher_course_types_course_types_CourseTypeId",
                table: "teacher_course_types");

            migrationBuilder.DropPrimaryKey(
                name: "PK_course_types",
                table: "course_types");

            // Add INT columns back
            migrationBuilder.AddColumn<int>(
                name: "OldId",
                table: "course_types",
                type: "integer",
                nullable: false)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<int>(
                name: "OldCourseTypeId",
                table: "courses",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OldCourseTypeId",
                table: "teacher_course_types",
                type: "integer",
                nullable: true);

            // Map UUIDs back to new INT IDs
            migrationBuilder.Sql(@"
                UPDATE courses c
                SET ""OldCourseTypeId"" = ct.""OldId""
                FROM course_types ct
                WHERE c.""CourseTypeId"" = ct.""Id"";
            ");

            migrationBuilder.Sql(@"
                UPDATE teacher_course_types tct
                SET ""OldCourseTypeId"" = ct.""OldId""
                FROM course_types ct
                WHERE tct.""CourseTypeId"" = ct.""Id"";
            ");

            // Drop UUID columns
            migrationBuilder.DropColumn(name: "CourseTypeId", table: "courses");
            migrationBuilder.DropColumn(name: "CourseTypeId", table: "teacher_course_types");
            migrationBuilder.DropColumn(name: "Id", table: "course_types");

            // Rename columns back
            migrationBuilder.RenameColumn(name: "OldId", table: "course_types", newName: "Id");
            migrationBuilder.RenameColumn(name: "OldCourseTypeId", table: "courses", newName: "CourseTypeId");
            migrationBuilder.RenameColumn(name: "OldCourseTypeId", table: "teacher_course_types", newName: "CourseTypeId");

            // Make non-nullable
            migrationBuilder.AlterColumn<int>(
                name: "CourseTypeId",
                table: "courses",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CourseTypeId",
                table: "teacher_course_types",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            // Recreate PK and FKs
            migrationBuilder.AddPrimaryKey(
                name: "PK_course_types",
                table: "course_types",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_courses_course_types_CourseTypeId",
                table: "courses",
                column: "CourseTypeId",
                principalTable: "course_types",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_teacher_course_types_course_types_CourseTypeId",
                table: "teacher_course_types",
                column: "CourseTypeId",
                principalTable: "course_types",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddPrimaryKey(
                name: "PK_teacher_course_types",
                table: "teacher_course_types",
                columns: new[] { "TeacherId", "CourseTypeId" });

            migrationBuilder.CreateIndex(
                name: "IX_teacher_course_types_CourseTypeId",
                table: "teacher_course_types",
                column: "CourseTypeId");
        }
    }
}
