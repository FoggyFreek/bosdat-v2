using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BosDAT.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoicingPreferenceToEnrollment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_invoices_StudentId",
                table: "invoices");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "invoices",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EnrollmentId",
                table: "invoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LedgerCreditsApplied",
                table: "invoices",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "LedgerDebitsApplied",
                table: "invoices",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateOnly>(
                name: "PeriodEnd",
                table: "invoices",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "PeriodStart",
                table: "invoices",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PeriodType",
                table: "invoices",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvoicingPreference",
                table: "enrollments",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Monthly");

            migrationBuilder.InsertData(
                table: "settings",
                columns: new[] { "Key", "Description", "Type", "Value" },
                values: new object[,]
                {
                    { "school_address", "School address", "string", "Muziekstraat 1" },
                    { "school_city", "School city", "string", "Zwolle" },
                    { "school_email", "School email", "string", "info@nmi-zwolle.nl" },
                    { "school_iban", "School IBAN bank account", "string", "NL00 BANK 0000 0000 00" },
                    { "school_kvk", "School KvK (Chamber of Commerce) number", "string", "12345678" },
                    { "school_phone", "School phone number", "string", "+31 38 123 4567" },
                    { "school_postal_code", "School postal code", "string", "8000 AB" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_invoices_EnrollmentId",
                table: "invoices",
                column: "EnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_invoices_Status",
                table: "invoices",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_invoices_StudentId_PeriodStart_PeriodEnd",
                table: "invoices",
                columns: new[] { "StudentId", "PeriodStart", "PeriodEnd" });

            migrationBuilder.AddForeignKey(
                name: "FK_invoices_enrollments_EnrollmentId",
                table: "invoices",
                column: "EnrollmentId",
                principalTable: "enrollments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_invoices_enrollments_EnrollmentId",
                table: "invoices");

            migrationBuilder.DropIndex(
                name: "IX_invoices_EnrollmentId",
                table: "invoices");

            migrationBuilder.DropIndex(
                name: "IX_invoices_Status",
                table: "invoices");

            migrationBuilder.DropIndex(
                name: "IX_invoices_StudentId_PeriodStart_PeriodEnd",
                table: "invoices");

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "Key",
                keyValue: "school_address");

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "Key",
                keyValue: "school_city");

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "Key",
                keyValue: "school_email");

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "Key",
                keyValue: "school_iban");

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "Key",
                keyValue: "school_kvk");

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "Key",
                keyValue: "school_phone");

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "Key",
                keyValue: "school_postal_code");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "EnrollmentId",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "LedgerCreditsApplied",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "LedgerDebitsApplied",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "PeriodEnd",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "PeriodStart",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "PeriodType",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "InvoicingPreference",
                table: "enrollments");

            migrationBuilder.CreateIndex(
                name: "IX_invoices_StudentId",
                table: "invoices",
                column: "StudentId");
        }
    }
}
