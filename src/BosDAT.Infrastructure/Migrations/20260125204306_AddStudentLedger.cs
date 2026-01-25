using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BosDAT.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "student_ledger_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CorrectionRefName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    EntryType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_student_ledger_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_student_ledger_entries_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_student_ledger_entries_courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_student_ledger_entries_students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "student_ledger_applications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LedgerEntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    AppliedAmount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    AppliedById = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_student_ledger_applications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_student_ledger_applications_AspNetUsers_AppliedById",
                        column: x => x.AppliedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_student_ledger_applications_invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_student_ledger_applications_student_ledger_entries_LedgerEn~",
                        column: x => x.LedgerEntryId,
                        principalTable: "student_ledger_entries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_student_ledger_applications_AppliedById",
                table: "student_ledger_applications",
                column: "AppliedById");

            migrationBuilder.CreateIndex(
                name: "IX_student_ledger_applications_InvoiceId",
                table: "student_ledger_applications",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_student_ledger_applications_LedgerEntryId",
                table: "student_ledger_applications",
                column: "LedgerEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_student_ledger_entries_CorrectionRefName",
                table: "student_ledger_entries",
                column: "CorrectionRefName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_student_ledger_entries_CourseId",
                table: "student_ledger_entries",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_student_ledger_entries_CreatedById",
                table: "student_ledger_entries",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_student_ledger_entries_EntryType",
                table: "student_ledger_entries",
                column: "EntryType");

            migrationBuilder.CreateIndex(
                name: "IX_student_ledger_entries_Status",
                table: "student_ledger_entries",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_student_ledger_entries_StudentId",
                table: "student_ledger_entries",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_student_ledger_entries_StudentId_Status",
                table: "student_ledger_entries",
                columns: new[] { "StudentId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "student_ledger_applications");

            migrationBuilder.DropTable(
                name: "student_ledger_entries");
        }
    }
}
