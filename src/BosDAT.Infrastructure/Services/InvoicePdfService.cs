using BosDAT.Core.DTOs;
using BosDAT.Core.Interfaces.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BosDAT.Infrastructure.Services;

public class InvoicePdfService : IInvoicePdfService
{
    public Task<byte[]> GeneratePdfAsync(InvoiceDto invoice, SchoolBillingInfoDto schoolInfo, CancellationToken ct = default)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginTop(1.5f, Unit.Centimetre);
                page.MarginBottom(1.5f, Unit.Centimetre);
                page.MarginHorizontal(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Element(header => ComposeHeader(header, invoice, schoolInfo));
                page.Content().Element(content => ComposeContent(content, invoice, schoolInfo));
                page.Footer().Element(ComposeFooter);
            });
        });

        var bytes = document.GeneratePdf();
        return Task.FromResult(bytes);
    }

    private static void ComposeHeader(IContainer container, InvoiceDto invoice, SchoolBillingInfoDto schoolInfo)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text(schoolInfo.Name).Bold().FontSize(16);

                if (!string.IsNullOrWhiteSpace(schoolInfo.Address))
                    col.Item().Text(schoolInfo.Address);
                if (!string.IsNullOrWhiteSpace(schoolInfo.PostalCode) || !string.IsNullOrWhiteSpace(schoolInfo.City))
                    col.Item().Text($"{schoolInfo.PostalCode} {schoolInfo.City}".Trim());
                if (!string.IsNullOrWhiteSpace(schoolInfo.Phone))
                    col.Item().Text($"Tel: {schoolInfo.Phone}");
                if (!string.IsNullOrWhiteSpace(schoolInfo.Email))
                    col.Item().Text($"E-mail: {schoolInfo.Email}");
                if (!string.IsNullOrWhiteSpace(schoolInfo.KvkNumber))
                    col.Item().Text($"KvK: {schoolInfo.KvkNumber}");
                if (!string.IsNullOrWhiteSpace(schoolInfo.BtwNumber))
                    col.Item().Text($"BTW: {schoolInfo.BtwNumber}");
            });

            row.ConstantItem(200).AlignRight().Column(col =>
            {
                var title = invoice.IsCreditInvoice ? "Creditfactuur" : "Factuur";
                col.Item().Text(title).Bold().FontSize(16);
                col.Item().Text($"#{invoice.InvoiceNumber}").FontSize(11);

                if (!string.IsNullOrWhiteSpace(invoice.Description))
                    col.Item().Text(invoice.Description).FontColor(Colors.Grey.Medium);

                if (invoice.IsCreditInvoice && !string.IsNullOrWhiteSpace(invoice.OriginalInvoiceNumber))
                    col.Item().Text($"Ref: {invoice.OriginalInvoiceNumber}").FontColor(Colors.Grey.Medium);
            });
        });
    }

    private static void ComposeContent(IContainer container, InvoiceDto invoice, SchoolBillingInfoDto schoolInfo)
    {
        container.PaddingVertical(15).Column(col =>
        {
            col.Spacing(15);

            // Billing info + dates
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(billTo =>
                {
                    billTo.Item().Text("Factuuradres:").Bold();
                    if (invoice.BillingContact != null)
                    {
                        billTo.Item().Text(invoice.BillingContact.Name);
                        if (!string.IsNullOrWhiteSpace(invoice.BillingContact.Address))
                            billTo.Item().Text(invoice.BillingContact.Address);
                        if (!string.IsNullOrWhiteSpace(invoice.BillingContact.PostalCode) || !string.IsNullOrWhiteSpace(invoice.BillingContact.City))
                            billTo.Item().Text($"{invoice.BillingContact.PostalCode} {invoice.BillingContact.City}".Trim());
                        if (!string.IsNullOrWhiteSpace(invoice.BillingContact.Email))
                            billTo.Item().Text(invoice.BillingContact.Email);
                    }
                });

                row.ConstantItem(200).AlignRight().Column(dates =>
                {
                    dates.Item().Text(text =>
                    {
                        text.Span("Factuurdatum: ").FontColor(Colors.Grey.Medium);
                        text.Span(invoice.IssueDate.ToString("dd-MM-yyyy"));
                    });
                    dates.Item().Text(text =>
                    {
                        text.Span("Vervaldatum: ").FontColor(Colors.Grey.Medium);
                        text.Span(invoice.DueDate.ToString("dd-MM-yyyy"));
                    });
                    if (invoice.PeriodStart.HasValue && invoice.PeriodEnd.HasValue)
                    {
                        dates.Item().Text(text =>
                        {
                            text.Span("Periode: ").FontColor(Colors.Grey.Medium);
                            text.Span($"{invoice.PeriodStart.Value:dd-MM-yyyy} - {invoice.PeriodEnd.Value:dd-MM-yyyy}");
                        });
                    }
                });
            });

            // Line items table
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(4); // Description
                    columns.RelativeColumn(1); // Qty
                    columns.RelativeColumn(1.5f); // Unit price
                    columns.RelativeColumn(1); // VAT
                    columns.RelativeColumn(1.5f); // Total
                });

                table.Header(header =>
                {
                    header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Medium)
                        .PaddingVertical(5).Text("Omschrijving").Bold();
                    header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Medium)
                        .PaddingVertical(5).AlignRight().Text("Aantal").Bold();
                    header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Medium)
                        .PaddingVertical(5).AlignRight().Text("Prijs").Bold();
                    header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Medium)
                        .PaddingVertical(5).AlignRight().Text("BTW").Bold();
                    header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Medium)
                        .PaddingVertical(5).AlignRight().Text("Totaal").Bold();
                });

                foreach (var line in invoice.Lines)
                {
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .PaddingVertical(4).Text(line.Description);
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .PaddingVertical(4).AlignRight().Text(line.Quantity.ToString());
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .PaddingVertical(4).AlignRight().Text(FormatCurrency(line.UnitPrice));
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .PaddingVertical(4).AlignRight().Text($"{line.VatRate:0}%");
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .PaddingVertical(4).AlignRight().Text(FormatCurrency(line.LineTotal));
                }
            });

            // Totals
            col.Item().AlignRight().Width(250).Column(totals =>
            {
                totals.Spacing(4);

                totals.Item().Row(r =>
                {
                    r.RelativeItem().Text("Subtotaal");
                    r.ConstantItem(100).AlignRight().Text(FormatCurrency(invoice.Subtotal));
                });

                totals.Item().Row(r =>
                {
                    r.RelativeItem().Text($"BTW ({schoolInfo.VatRate}%)");
                    r.ConstantItem(100).AlignRight().Text(FormatCurrency(invoice.VatAmount));
                });

                if (invoice.DiscountAmount != 0)
                {
                    totals.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Korting");
                        r.ConstantItem(100).AlignRight().Text($"-{FormatCurrency(invoice.DiscountAmount)}");
                    });
                }

                totals.Item().BorderTop(1).BorderColor(Colors.Black).PaddingTop(4).Row(r =>
                {
                    var label = invoice.IsCreditInvoice ? "Totaal credit" : "Totaal te betalen";
                    r.RelativeItem().Text(label).Bold().FontSize(11);
                    r.ConstantItem(100).AlignRight().Text(FormatCurrency(invoice.Total)).Bold().FontSize(11);
                });

                if (!invoice.IsCreditInvoice && invoice.AmountPaid > 0)
                {
                    totals.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Betaald").FontColor(Colors.Green.Medium);
                        r.ConstantItem(100).AlignRight().Text($"-{FormatCurrency(invoice.AmountPaid)}").FontColor(Colors.Green.Medium);
                    });

                    totals.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Openstaand").Bold();
                        r.ConstantItem(100).AlignRight().Text(FormatCurrency(invoice.Balance)).Bold();
                    });
                }
            });

            // Payment instructions / credit note
            col.Item().BorderTop(1).BorderColor(Colors.Grey.Lighten1).PaddingTop(10).Column(footer =>
            {
                if (invoice.IsCreditInvoice)
                {
                    footer.Item().Text("Creditnota").Bold();
                    footer.Item().Text("Dit is een creditfactuur. Het bedrag wordt verrekend met openstaande facturen.")
                        .FontColor(Colors.Grey.Medium);
                }
                else
                {
                    footer.Item().Text("Betalingsgegevens").Bold();
                    footer.Item().Text($"Gelieve het bedrag over te maken onder vermelding van factuurnummer {invoice.InvoiceNumber}.")
                        .FontColor(Colors.Grey.Medium);

                    if (!string.IsNullOrWhiteSpace(schoolInfo.Iban))
                    {
                        footer.Item().PaddingTop(4).Text(text =>
                        {
                            text.Span("IBAN: ").FontColor(Colors.Grey.Medium);
                            text.Span(schoolInfo.Iban);
                        });
                    }
                }
            });
        });
    }

    private static void ComposeFooter(IContainer container)
    {
        container.AlignCenter().Text(text =>
        {
            text.CurrentPageNumber();
            text.Span(" / ");
            text.TotalPages();
        });
    }

    private static string FormatCurrency(decimal amount)
    {
        return $"€ {amount:N2}";
    }
}
