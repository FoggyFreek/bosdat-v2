using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BosDAT.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditInvoiceFields : Migration
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

            migrationBuilder.CreateIndex(
                name: "IX_invoices_OriginalInvoiceId",
                table: "invoices",
                column: "OriginalInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_lines_OriginalInvoiceLineId",
                table: "invoice_lines",
                column: "OriginalInvoiceLineId");

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

            migrationBuilder.DropIndex(
                name: "IX_invoice_lines_OriginalInvoiceLineId",
                table: "invoice_lines");

            migrationBuilder.DropIndex(
                name: "IX_invoices_OriginalInvoiceId",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "OriginalInvoiceLineId",
                table: "invoice_lines");

            migrationBuilder.DropColumn(
                name: "OriginalInvoiceId",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "IsCreditInvoice",
                table: "invoices");
        }
    }
}
