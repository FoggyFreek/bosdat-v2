using Microsoft.EntityFrameworkCore;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Core.Interfaces;
using BosDAT.Infrastructure.Data;
using System.Transactions;

namespace BosDAT.Infrastructure.Services;

public class InvoiceLedgerService(
    ApplicationDbContext context,
    IUnitOfWork unitOfWork,
    IInvoiceQueryService queryService,
    IStudentTransactionService studentTransactionService) : IInvoiceLedgerService
{
    public async Task<InvoiceDto> ApplyLedgerCorrectionAsync(
        Guid invoiceId, Guid ledgerEntryId, decimal amount, Guid userId, CancellationToken ct = default)
    {
        var invoice = await context.Invoices
            .Include(i => i.Payments)
            .Include(i => i.LedgerApplications)
            .FirstOrDefaultAsync(i => i.Id == invoiceId, ct)
            ?? throw new InvalidOperationException($"Invoice with ID {invoiceId} not found.");

        if (invoice.Status == InvoiceStatus.Paid || invoice.Status == InvoiceStatus.Cancelled)
        {
            throw new InvalidOperationException($"Cannot apply correction to invoice with status {invoice.Status}.");
        }

        var ledgerEntry = await context.StudentLedgerEntries
            .Include(e => e.Applications)
            .FirstOrDefaultAsync(e => e.Id == ledgerEntryId, ct)
            ?? throw new InvalidOperationException($"Ledger entry with ID {ledgerEntryId} not found.");

        if (ledgerEntry.StudentId != invoice.StudentId)
        {
            throw new InvalidOperationException("Ledger entry does not belong to the invoice's student.");
        }

        var availableAmount = GetAvailableAmount(ledgerEntry);

        if (amount > availableAmount)
        {
            throw new InvalidOperationException($"Requested amount ({amount}) exceeds available amount ({availableAmount}).");
        }

        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            ApplyEntryToInvoice(ledgerEntry, invoice, amount, userId, ct);

            // Check if invoice is fully paid
            var totalPaid = invoice.Payments.Sum(p => p.Amount) + invoice.LedgerCreditsApplied;
            var totalOwed = invoice.Total + invoice.LedgerDebitsApplied;

            if (totalPaid >= totalOwed && invoice.Status != InvoiceStatus.Paid)
            {
                invoice.Status = InvoiceStatus.Paid;
                invoice.PaidAt = DateTime.UtcNow;
            }

            await context.SaveChangesAsync(ct);
            await studentTransactionService.RecordCorrectionAppliedAsync(ledgerEntry, invoice, amount, userId, ct);
            await unitOfWork.CommitTransactionAsync(ct);

            return await queryService.GetInvoiceAsync(invoiceId, ct)
                ?? throw new InvalidOperationException("Failed to retrieve invoice after applying ledger correction.");
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }
    }

    public async Task ApplyLedgerCorrectionsToInvoiceAsync(
        Invoice invoice, Guid studentId, Guid userId, CancellationToken ct = default)
    {
        var openEntries = await context.StudentLedgerEntries
            .Include(e => e.Applications)
            .Where(e => e.StudentId == studentId &&
                        (e.Status == LedgerEntryStatus.Open || e.Status == LedgerEntryStatus.PartiallyApplied))
            .ToListAsync(ct);

        // Apply debits first (increase amount owed)
        foreach (var debit in openEntries.Where(e => e.EntryType == LedgerEntryType.Debit))
        {
            var available = GetAvailableAmount(debit);
            if (available <= 0) continue;
            ApplyEntryToInvoice(debit, invoice, available, userId, ct);
        }

        // Apply credits (reduce amount owed)
        var subTotal = invoice.Subtotal + invoice.LedgerDebitsApplied;
        foreach (var credit in openEntries.Where(e => e.EntryType == LedgerEntryType.Credit))
        {
            if (subTotal <= 0) break;
            var available = GetAvailableAmount(credit);
            if (available <= 0) continue;
            var amount = Math.Min(available, subTotal);
            ApplyEntryToInvoice(credit, invoice, amount, userId, ct);
            subTotal -= amount;
        }

        //if the correction leads to a zero subtotal, mark invoice as paid
        CheckAndMarkPaid(invoice);
    }

    public async Task RevertLedgerApplicationsAsync(Invoice invoice, CancellationToken ct = default)
    {
        foreach (var application in invoice.LedgerApplications.ToList())
        {
            var ledgerEntry = await context.StudentLedgerEntries
                .Include(e => e.Applications)
                .FirstOrDefaultAsync(e => e.Id == application.LedgerEntryId, ct);

            if (ledgerEntry != null)
            {
                RecalculateLedgerEntryStatus(ledgerEntry, application.Id);
            }

            context.StudentLedgerApplications.Remove(application);
        }

        invoice.LedgerCreditsApplied =0;
        invoice.LedgerDebitsApplied =0;
    }

    private static decimal GetAvailableAmount(StudentLedgerEntry entry)
        => entry.Amount - entry.Applications.Sum(a => a.AppliedAmount);

    private void ApplyEntryToInvoice(
        StudentLedgerEntry entry, Invoice invoice, decimal amount, Guid userId, CancellationToken ct = default)
    {
        var application = new StudentLedgerApplication
        {
            Id = Guid.NewGuid(),
            LedgerEntryId = entry.Id,   
            InvoiceId = invoice.Id,
            AppliedAmount = amount,
            AppliedById = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.StudentLedgerApplications.Add(application);
        invoice.LedgerApplications.Add(application);
        studentTransactionService.RecordCorrectionAppliedAsync(entry, invoice, amount, userId, ct);

        if (entry.EntryType == LedgerEntryType.Debit)
            invoice.LedgerDebitsApplied += amount;
        else
            invoice.LedgerCreditsApplied += amount;

        UpdateLedgerEntryStatus(entry, amount);
    }

    private static void UpdateLedgerEntryStatus(StudentLedgerEntry entry, decimal newlyApplied)
    {
        var totalApplied = entry.Applications.Sum(a => a.AppliedAmount) + newlyApplied;
        entry.Status = totalApplied >= entry.Amount
            ? LedgerEntryStatus.FullyApplied
            : LedgerEntryStatus.PartiallyApplied;
    }

    private static void CheckAndMarkPaid(Invoice invoice)
    {
        var amountOwed = invoice.Subtotal + invoice.LedgerDebitsApplied;
        if (invoice.LedgerCreditsApplied >= amountOwed)
        {
            invoice.Status = InvoiceStatus.Paid;
            invoice.PaidAt = DateTime.UtcNow;
        }
    }

    private static void RecalculateLedgerEntryStatus(StudentLedgerEntry entry, Guid excludedApplicationId)
    {
        var remainingApplied = entry.Applications
            .Where(a => a.Id != excludedApplicationId)
            .Sum(a => a.AppliedAmount);

        if (remainingApplied <= 0)
            entry.Status = LedgerEntryStatus.Open;
        else if (remainingApplied < entry.Amount)
            entry.Status = LedgerEntryStatus.PartiallyApplied;
        else
            entry.Status = LedgerEntryStatus.FullyApplied;
    }
}
