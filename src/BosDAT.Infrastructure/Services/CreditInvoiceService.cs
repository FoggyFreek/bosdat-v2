using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;

namespace BosDAT.Infrastructure.Services;

public class CreditInvoiceService(
    IUnitOfWork unitOfWork,
    IStudentTransactionService studentTransactionService,
    IInvoiceQueryService queryService) : ICreditInvoiceService
{
    public async Task<InvoiceDto> CreateCreditInvoiceAsync(
        Guid originalInvoiceId, CreateCreditInvoiceDto dto, Guid userId, CancellationToken ct = default)
    {
        var originalInvoice = await unitOfWork.Invoices.GetWithLinesAsync(originalInvoiceId, ct)
            ?? throw new InvalidOperationException("Original invoice not found.");

        if (originalInvoice.Status == InvoiceStatus.Draft)
            throw new InvalidOperationException("Cannot create a credit invoice for a draft invoice.");

        if (originalInvoice.IsCreditInvoice)
            throw new InvalidOperationException("Cannot create a credit invoice for another credit invoice.");

        if (dto.SelectedLineIds.Count == 0)
            throw new InvalidOperationException("At least one invoice line must be selected for crediting.");

        var selectedLines = originalInvoice.Lines
            .Where(l => dto.SelectedLineIds.Contains(l.Id))
            .ToList();

        if (selectedLines.Count != dto.SelectedLineIds.Count)
            throw new InvalidOperationException("One or more selected line IDs do not belong to this invoice.");

        try
        {
            await unitOfWork.BeginTransactionAsync(ct);

            var creditInvoiceNumber = await unitOfWork.Invoices.GenerateCreditInvoiceNumberAsync(ct);
            var vatRate = await queryService.GetVatRateAsync(ct);

            var creditInvoice = new Invoice
            {
                Id = Guid.NewGuid(),
                InvoiceNumber = creditInvoiceNumber,
                StudentId = originalInvoice.StudentId,
                EnrollmentId = originalInvoice.EnrollmentId,
                IssueDate = DateOnly.FromDateTime(DateTime.UtcNow),
                DueDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Description = $"Credit {originalInvoice.InvoiceNumber}",
                PeriodStart = originalInvoice.PeriodStart,
                PeriodEnd = originalInvoice.PeriodEnd,
                PeriodType = originalInvoice.PeriodType,
                Status = InvoiceStatus.Draft,
                IsCreditInvoice = true,
                OriginalInvoiceId = originalInvoice.Id,
                Notes = BuildCreditNotes(originalInvoice.InvoiceNumber, dto.Notes)
            };

            foreach (var originalLine in selectedLines)
            {
                var creditLine = new InvoiceLine
                {
                    InvoiceId = creditInvoice.Id,
                    LessonId = originalLine.LessonId,
                    PricingVersionId = originalLine.PricingVersionId,
                    OriginalInvoiceLineId = originalLine.Id,
                    Description = originalLine.Description,
                    Quantity = originalLine.Quantity,
                    UnitPrice = -originalLine.UnitPrice,
                    VatRate = originalLine.VatRate,
                    VatAmount = -originalLine.VatAmount,
                    LineTotal = -originalLine.LineTotal
                };
                creditInvoice.Lines.Add(creditLine);
            }

            creditInvoice.CalculateInvoiceTotals();

            await unitOfWork.Invoices.AddAsync(creditInvoice, ct);
            await unitOfWork.SaveChangesAsync(ct);
            await unitOfWork.CommitTransactionAsync(ct);

            var result = await queryService.GetInvoiceAsync(creditInvoice.Id, ct);
            return result!;
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }
    }

    public async Task<InvoiceDto> ConfirmCreditInvoiceAsync(
        Guid creditInvoiceId, Guid userId, CancellationToken ct = default)
    {
        var creditInvoice = await unitOfWork.Invoices.GetWithLinesAsync(creditInvoiceId, ct)
            ?? throw new InvalidOperationException("Credit invoice not found.");

        if (!creditInvoice.IsCreditInvoice)
            throw new InvalidOperationException("This invoice is not a credit invoice.");

        if (creditInvoice.Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Only draft credit invoices can be confirmed.");

        if (creditInvoice.OriginalInvoiceId == null)
            throw new InvalidOperationException("Credit invoice must reference an original invoice.");

        var originalInvoice = await unitOfWork.Invoices.GetWithLinesAsync(creditInvoice.OriginalInvoiceId.Value, ct)
            ?? throw new InvalidOperationException("Original invoice not found.");

        try
        {
            await unitOfWork.BeginTransactionAsync(ct);

            creditInvoice.Status = InvoiceStatus.Sent;
            await unitOfWork.SaveChangesAsync(ct);

            await studentTransactionService.RecordCreditInvoiceAsync(creditInvoice, originalInvoice, userId, ct);
            await unitOfWork.CommitTransactionAsync(ct);

            var result = await queryService.GetInvoiceAsync(creditInvoice.Id, ct);
            return result!;
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }
    }

    public async Task<InvoiceDto> ApplyCreditInvoicesAsync(
        Guid invoiceId, Guid userId, CancellationToken ct = default)
    {
        var invoice = await unitOfWork.Invoices.GetWithLinesAsync(invoiceId, ct)
            ?? throw new InvalidOperationException("Invoice not found.");

        if (invoice.IsCreditInvoice)
            throw new InvalidOperationException("Cannot apply credit to a credit invoice.");

        if (invoice.Status == InvoiceStatus.Cancelled)
            throw new InvalidOperationException("Cannot apply credit to a Cancelled invoice.");

        if (invoice.Status == InvoiceStatus.Paid)
            throw new InvalidOperationException("Invoice is already paid.");

        var invoiceRemaining = invoice.Total - invoice.Payments.Sum(p => p.Amount);
        if (invoiceRemaining <= 0)
            throw new InvalidOperationException("Invoice has no remaining balance.");

        var creditInvoices = await unitOfWork.Invoices.GetConfirmedCreditInvoicesWithRemainingCreditAsync(invoice.StudentId, ct);
        if (creditInvoices.Count == 0)
            throw new InvalidOperationException("No credit invoices with remaining credit available.");

        try
        {
            await unitOfWork.BeginTransactionAsync(ct);

            foreach (var creditInvoice in creditInvoices)
            {
                if (invoiceRemaining <= 0) break;

                var creditRemaining = Math.Abs(creditInvoice.Total)
                    - await unitOfWork.StudentTransactions.GetAppliedCreditAmountAsync(creditInvoice.Id, ct);

                if (creditRemaining <= 0) continue;

                var amountToApply = Math.Min(creditRemaining, invoiceRemaining);

                var payment = new Payment
                {
                    Id = Guid.NewGuid(),
                    InvoiceId = invoice.Id,
                    Amount = amountToApply,
                    PaymentDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    Method = PaymentMethod.CreditBalance,
                    Reference = creditInvoice.InvoiceNumber,
                    RecordedById = userId
                };

                await unitOfWork.Repository<Payment>().AddAsync(payment, ct);

                invoiceRemaining -= amountToApply;

                if (invoiceRemaining <= 0)
                {
                    invoice.Status = InvoiceStatus.Paid;
                    invoice.PaidAt = DateTime.UtcNow;
                }

                await unitOfWork.SaveChangesAsync(ct);
                await studentTransactionService.RecordCreditAppliedAsync(creditInvoice, invoice, payment, userId, ct);
            }

            await unitOfWork.CommitTransactionAsync(ct);

            var result = await queryService.GetInvoiceAsync(invoice.Id, ct);
            return result!;
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }
    }

    private static string BuildCreditNotes(string originalInvoiceNumber, string? additionalNotes)
    {
        var note = $"Creditfactuur voor factuur {originalInvoiceNumber}";
        if (!string.IsNullOrWhiteSpace(additionalNotes))
            note += $"\n{additionalNotes}";
        return note;
    }
}
