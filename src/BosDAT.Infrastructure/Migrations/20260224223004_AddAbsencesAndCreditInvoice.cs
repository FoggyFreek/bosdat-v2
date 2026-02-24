using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BosDAT.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAbsencesAndCreditInvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCreditInvoice",
                table: "invoices",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "OriginalInvoiceId",
                table: "invoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OriginalInvoiceLineId",
                table: "invoice_lines",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "absences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: true),
                    TeacherId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Reason = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    InvoiceLesson = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_absences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_absences_students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_absences_teachers_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "settings",
                columns: new[] { "Key", "Description", "Type", "Value" },
                values: new object[] { "school_btw", "School BTW-identificatienummer (VAT ID)", "string", "NL000000000B00" });

            migrationBuilder.CreateIndex(
                name: "IX_invoices_OriginalInvoiceId",
                table: "invoices",
                column: "OriginalInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_lines_OriginalInvoiceLineId",
                table: "invoice_lines",
                column: "OriginalInvoiceLineId");

            migrationBuilder.CreateIndex(
                name: "IX_absences_StudentId_StartDate_EndDate",
                table: "absences",
                columns: new[] { "StudentId", "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_absences_TeacherId_StartDate_EndDate",
                table: "absences",
                columns: new[] { "TeacherId", "StartDate", "EndDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_invoices_invoices_OriginalInvoiceId",
                table: "invoices",
                column: "OriginalInvoiceId",
                principalTable: "invoices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_invoices_invoices_OriginalInvoiceId",
                table: "invoices");

            migrationBuilder.DropTable(
                name: "absences");

            migrationBuilder.DropIndex(
                name: "IX_invoices_OriginalInvoiceId",
                table: "invoices");

            migrationBuilder.DropIndex(
                name: "IX_invoice_lines_OriginalInvoiceLineId",
                table: "invoice_lines");

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "Key",
                keyValue: "school_btw");

            migrationBuilder.DropColumn(
                name: "IsCreditInvoice",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "OriginalInvoiceId",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "OriginalInvoiceLineId",
                table: "invoice_lines");
        }
    }
}
