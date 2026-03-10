using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BosDAT.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceEmailSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Value",
                table: "settings",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentsJson",
                table: "email_outbox_messages",
                type: "jsonb",
                nullable: true);

            migrationBuilder.InsertData(
                table: "settings",
                columns: new[] { "Key", "Description", "Type", "Value" },
                values: new object[,]
                {
                    { "email_invoice_body_template", "Razor HTML body template for invoice emails. Placeholders: @Model.InvoiceNumber, @Model.SchoolName, @Model.StudentFirstName, @Model.StudentLastName, @Model.Total, @Model.DueDate, @Model.IssueDate", "template", "<!DOCTYPE html>\r\n<html lang=\"nl\">\r\n<head>\r\n    <meta charset=\"utf-8\" />\r\n    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\" />\r\n    <style>\r\n        body { margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; background-color: #f4f4f5; color: #18181b; }\r\n        .container { max-width: 600px; margin: 0 auto; padding: 40px 20px; }\r\n        .card { background: #ffffff; border-radius: 8px; padding: 32px; box-shadow: 0 1px 3px rgba(0,0,0,0.1); }\r\n        .header { text-align: center; margin-bottom: 24px; }\r\n        .header h1 { font-size: 24px; font-weight: 600; margin: 0; color: #18181b; }\r\n        .content { line-height: 1.6; color: #3f3f46; }\r\n        .content p { margin: 0 0 16px 0; }\r\n        .details { background: #f4f4f5; border-radius: 4px; padding: 16px; margin: 16px 0; }\r\n        .details table { width: 100%; border-collapse: collapse; }\r\n        .details td { padding: 4px 0; }\r\n        .details td:last-child { text-align: right; font-weight: 500; }\r\n        .footer { text-align: center; margin-top: 24px; font-size: 12px; color: #a1a1aa; }\r\n    </style>\r\n</head>\r\n<body>\r\n    <div class=\"container\">\r\n        <div class=\"card\">\r\n            <div class=\"header\">\r\n                <h1>@Model.SchoolName</h1>\r\n            </div>\r\n            <div class=\"content\">\r\n                <p>Beste @Model.StudentFirstName,</p>\r\n                <p>Bijgaand vindt u de factuur voor uw muziekles.</p>\r\n                <div class=\"details\">\r\n                    <table>\r\n                        <tr><td>Factuurnummer</td><td>@Model.InvoiceNumber</td></tr>\r\n                        <tr><td>Factuurdatum</td><td>@Model.IssueDate</td></tr>\r\n                        <tr><td>Vervaldatum</td><td>@Model.DueDate</td></tr>\r\n                        <tr><td><strong>Totaalbedrag</strong></td><td><strong>€ @Model.Total</strong></td></tr>\r\n                    </table>\r\n                </div>\r\n                <p>De factuur is als PDF bijgevoegd bij deze e-mail.</p>\r\n                <p>Wij verzoeken u vriendelijk het bedrag voor de vervaldatum over te maken.</p>\r\n                <p>Met vriendelijke groet,<br />@Model.SchoolName</p>\r\n            </div>\r\n        </div>\r\n        <div class=\"footer\">\r\n            <p>Dit is een automatisch gegenereerd bericht.</p>\r\n        </div>\r\n    </div>\r\n</body>\r\n</html>" },
                    { "email_invoice_subject_template", "Subject template for invoice emails. Placeholders: {{InvoiceNumber}}, {{SchoolName}}, {{StudentFirstName}}, {{StudentLastName}}, {{Total}}, {{DueDate}}, {{IssueDate}}", "string", "Factuur {{InvoiceNumber}} - {{SchoolName}}" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "Key",
                keyValue: "email_invoice_body_template");

            migrationBuilder.DeleteData(
                table: "settings",
                keyColumn: "Key",
                keyValue: "email_invoice_subject_template");

            migrationBuilder.DropColumn(
                name: "AttachmentsJson",
                table: "email_outbox_messages");

            migrationBuilder.AlterColumn<string>(
                name: "Value",
                table: "settings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
