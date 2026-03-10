using System.Security.Cryptography;
using System.Text;
using BosDAT.Core.Constants;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Exceptions;
using BosDAT.Core.Interfaces;
using BosDAT.Core.Interfaces.Services;

namespace BosDAT.Infrastructure.Services;

public class InvoiceEmailService(
    IInvoiceService invoiceService,
    IInvoicePdfService invoicePdfService,
    IEmailTemplateRenderer emailTemplateRenderer,
    ISettingsService settingsService,
    IEmailService emailService,
    IUnitOfWork uow) : IInvoiceEmailService
{
    private const string SubjectSettingKey = "email_invoice_subject_template";
    private const string BodySettingKey = "email_invoice_body_template";

    public virtual async Task<InvoiceEmailPreviewDto> PreviewAsync(Guid invoiceId, CancellationToken ct = default)
    {
        var invoice = await invoiceService.GetInvoiceAsync(invoiceId, ct)
            ?? throw new NotFoundException("Invoice not found.");

        var toEmail = GetRecipientEmail(invoice);
        var schoolInfo = await invoiceService.GetSchoolBillingInfoAsync(ct);
        var (subject, htmlBody) = await RenderEmailAsync(invoice, schoolInfo, ct);

        return new InvoiceEmailPreviewDto(htmlBody, subject, toEmail);
    }

    public virtual async Task<InvoiceDto> SendAsync(Guid invoiceId, CancellationToken ct = default)
    {
        var invoice = await invoiceService.GetInvoiceAsync(invoiceId, ct)
            ?? throw new NotFoundException("Invoice not found.");

        if (invoice.IsCreditInvoice)
            throw new InvalidOperationException("Cannot send email for credit invoices.");

        var toEmail = GetRecipientEmail(invoice);
        var schoolInfo = await invoiceService.GetSchoolBillingInfoAsync(ct);
        var (subject, htmlBody) = await RenderEmailAsync(invoice, schoolInfo, ct);

        var pdfBytes = await invoicePdfService.GeneratePdfAsync(invoice, schoolInfo, ct);
        var pdfBase64 = Convert.ToBase64String(pdfBytes);

        var attachment = new EmailAttachment($"{invoice.InvoiceNumber}.pdf", pdfBase64);

        await emailService.QueueEmailAsync(
            toEmail,
            subject,
            EmailOutboxConstants.RenderedTemplateName,
            new { __html__ = htmlBody },
            [attachment],
            ct);

        if (invoice.Status == InvoiceStatus.Draft)
        {
            var entity = await uow.Invoices.GetByIdAsync(invoiceId, ct)
                ?? throw new NotFoundException("Invoice entity not found.");
            entity.Status = InvoiceStatus.Sent;
        }

        await uow.SaveChangesAsync(ct);

        return await invoiceService.GetInvoiceAsync(invoiceId, ct)
            ?? throw new NotFoundException("Invoice not found after send.");
    }

    private string GetRecipientEmail(InvoiceDto invoice)
    {
        var email = invoice.BillingContact?.Email ?? invoice.StudentEmail;
        if (string.IsNullOrWhiteSpace(email))
            throw new InvalidOperationException("Student has no email address.");
        return email;
    }

    private async Task<(string Subject, string HtmlBody)> RenderEmailAsync(
        InvoiceDto invoice, SchoolBillingInfoDto schoolInfo, CancellationToken ct)
    {
        var subjectSetting = await settingsService.GetByKeyAsync(SubjectSettingKey, ct);
        var bodySetting = await settingsService.GetByKeyAsync(BodySettingKey, ct);

        var subjectTemplate = subjectSetting?.Value ?? "Factuur {{InvoiceNumber}}";
        var bodyTemplate = bodySetting?.Value ?? "<p>Beste {{StudentFirstName}},</p><p>Bijgaand vindt u uw factuur.</p>";

        var model = new
        {
            StudentFirstName = invoice.StudentName.Split(' ')[0],
            StudentLastName = invoice.StudentName.Contains(' ')
                ? invoice.StudentName[(invoice.StudentName.IndexOf(' ') + 1)..]
                : invoice.StudentName,
            invoice.InvoiceNumber,
            invoice.Total,
            DueDate = invoice.DueDate.ToString("dd-MM-yyyy"),
            IssueDate = invoice.IssueDate.ToString("dd-MM-yyyy"),
            SchoolName = schoolInfo.Name
        };

        var subject = subjectTemplate
            .Replace("{{InvoiceNumber}}", model.InvoiceNumber)
            .Replace("{{SchoolName}}", model.SchoolName)
            .Replace("{{StudentFirstName}}", model.StudentFirstName)
            .Replace("{{StudentLastName}}", model.StudentLastName)
            .Replace("{{Total}}", model.Total.ToString("F2"))
            .Replace("{{DueDate}}", model.DueDate)
            .Replace("{{IssueDate}}", model.IssueDate);

        var contentHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(bodyTemplate)))[..16];
        var cacheKey = $"invoice_email_{contentHash}";

        var htmlBody = await emailTemplateRenderer.RenderFromContentAsync(bodyTemplate, cacheKey, model, ct);

        return (subject, htmlBody);
    }
}
