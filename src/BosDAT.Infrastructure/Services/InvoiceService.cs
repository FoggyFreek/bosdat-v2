using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Core.Interfaces;

namespace BosDAT.Infrastructure.Services;

public class InvoiceService(
    IUnitOfWork unitOfWork,
    IStudentTransactionService studentTransactionService,
    IInvoiceGenerationService generation,
    IInvoiceQueryService query) : IInvoiceService
{

    public Task<InvoiceDto> GenerateInvoiceAsync(GenerateInvoiceDto dto, Guid userId, CancellationToken ct = default)
        => generation.GenerateInvoiceAsync(dto, userId, ct);

    public Task<IReadOnlyList<InvoiceDto>> GenerateBatchInvoicesAsync(GenerateBatchInvoicesDto dto, Guid userId, CancellationToken ct = default)
        => generation.GenerateBatchInvoicesAsync(dto, userId, ct);

    public Task<InvoiceDto> RecalculateInvoiceAsync(Guid invoiceId, Guid userId, CancellationToken ct = default)
        => generation.RecalculateInvoiceAsync(invoiceId, userId, ct);

    public Task<InvoiceDto?> GetInvoiceAsync(Guid invoiceId, CancellationToken ct = default)
        => query.GetInvoiceAsync(invoiceId, ct);

    public Task<InvoiceDto?> GetByInvoiceNumberAsync(string invoiceNumber, CancellationToken ct = default)
        => query.GetByInvoiceNumberAsync(invoiceNumber, ct);

    public Task<IReadOnlyList<InvoiceListDto>> GetStudentInvoicesAsync(Guid studentId, CancellationToken ct = default)
        => query.GetStudentInvoicesAsync(studentId, ct);

    public Task<IReadOnlyList<InvoiceListDto>> GetByStatusAsync(InvoiceStatus status, CancellationToken ct = default)
        => query.GetByStatusAsync(status, ct);

    public async Task<PaymentDto> RecordPaymentandLedgerTransaction(Guid invoiceId, Guid userId, RecordPaymentDto dto, CancellationToken ct = default)
    {
        var invoice = await unitOfWork.Invoices.GetWithLinesAsync(invoiceId, ct);
        if (invoice == null)
        {
            throw new ApplicationException("Invoice not found.");
        }

        if (invoice.Status == InvoiceStatus.Cancelled)
        {
            throw new ApplicationException("Cannot record payment for a cancelled invoice.");
        }

        var existingPayments = invoice.Payments?.Sum(p => p.Amount) ?? 0;
        var remainingBalance = invoice.Total - existingPayments;

        if (dto.Amount > remainingBalance)
        {
            throw new ApplicationException($"Payment amount exceeds remaining balance of {remainingBalance:F2}.");
        }

        try
        {
            await unitOfWork.BeginTransactionAsync(ct);

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                InvoiceId = invoiceId,
                Amount = dto.Amount,
                PaymentDate = dto.PaymentDate,
                Method = dto.Method,
                Reference = dto.Reference,
                Notes = dto.Notes,
                RecordedById = userId
            };

            await unitOfWork.Repository<Payment>().AddAsync(payment, ct);

            var totalPaidAfter = existingPayments + dto.Amount;
            if (totalPaidAfter >= invoice.Total)
            {
                invoice.Status = InvoiceStatus.Paid;
                invoice.PaidAt = DateTime.UtcNow;
            }

            await unitOfWork.SaveChangesAsync(ct);
            await studentTransactionService.RecordPaymentAsync(payment, invoice, userId, ct);
            await unitOfWork.CommitTransactionAsync(ct);

            var user = await unitOfWork.Repository<ApplicationUser>().GetByIdAsync(userId, ct);

            return new PaymentDto
            {
                Id = payment.Id,
                InvoiceId = payment.InvoiceId,
                Amount = payment.Amount,
                PaymentDate = payment.PaymentDate,
                Method = payment.Method,
                Reference = payment.Reference,
                Notes = payment.Notes,
                RecordedByName = user != null ? $"{user.FirstName} {user.LastName}".Trim() : "Unknown",
                CreatedAt = payment.CreatedAt
            };
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }
    }

    public async Task<IEnumerable<PaymentDto>> GetPaymentsAsync(Guid invoiceId, CancellationToken ct = default)
    {
        var invoice = await unitOfWork.Invoices.GetWithLinesAsync(invoiceId, ct);
        if (invoice == null)
        {
            throw new ApplicationException("Invoice not found.");
        }

        var payments = invoice.Payments ?? [];
        var userIds = payments
            .Where(p => p.RecordedById.HasValue)
            .Select(p => p.RecordedById!.Value)
            .Distinct()
            .ToList();

        var users = new Dictionary<Guid, ApplicationUser>();
        foreach (var uid in userIds)
        {
            var user = await unitOfWork.Repository<ApplicationUser>().GetByIdAsync(uid, ct);
            if (user != null) users[uid] = user;
        }

        var paymentDtos = payments.Select(p =>
        {
            var recordedByName = "Unknown";
            if (p.RecordedById.HasValue && users.TryGetValue(p.RecordedById.Value, out var user))
            {
                recordedByName = $"{user.FirstName} {user.LastName}".Trim();
                if (string.IsNullOrWhiteSpace(recordedByName))
                    recordedByName = user.Email ?? "Unknown";
            }

            return MapToDto(p, recordedByName);
        }).ToList();

        return paymentDtos;
    }

    public string GeneratePeriodDescription(DateOnly periodStart, DateOnly periodEnd, InvoicingPreference periodType)
        => query.GeneratePeriodDescription(periodStart, periodEnd, periodType);

    public Task<SchoolBillingInfoDto> GetSchoolBillingInfoAsync(CancellationToken ct = default)
        => query.GetSchoolBillingInfoAsync(ct);

    internal static PaymentDto MapToDto(Payment payment, string recordedByName)
    {
        return new PaymentDto
        {
            Id = payment.Id,
            InvoiceId = payment.InvoiceId,
            Amount = payment.Amount,
            PaymentDate = payment.PaymentDate,
            Method = payment.Method,
            Reference = payment.Reference,
            Notes = payment.Notes,
            RecordedByName = recordedByName,
            CreatedAt = payment.CreatedAt
        };
    }
}
