using Microsoft.EntityFrameworkCore;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;
using BosDAT.Infrastructure.Data;
using BosDAT.Infrastructure.Utilities;

namespace BosDAT.Infrastructure.Services;

public class StudentLedgerService(
    ApplicationDbContext context,
    IStudentLedgerRepository ledgerRepository,
    IUnitOfWork unitOfWork) : IStudentLedgerService
{
    private const int MaxDescriptionLength = 500;
    private const string UnknownUserName = "Unknown";

    public async Task<StudentLedgerEntryDto> CreateEntryAsync(CreateStudentLedgerEntryDto dto, Guid userId, CancellationToken ct = default)
    {
        // HIGH-1: Validate input
        if (dto.Amount <= 0)
        {
            throw new ArgumentException("Amount must be greater than zero.", nameof(dto));
        }

        if (string.IsNullOrWhiteSpace(dto.Description))
        {
            throw new ArgumentException("Description is required.", nameof(dto));
        }

        if (dto.Description.Length > MaxDescriptionLength)
        {
            throw new ArgumentException($"Description cannot exceed {MaxDescriptionLength} characters.", nameof(dto));
        }

        var student = await context.Students.FindAsync([dto.StudentId], ct)
            ?? throw new InvalidOperationException($"Student with ID {dto.StudentId} not found.");

        if (dto.CourseId.HasValue)
        {
            var courseExists = await context.Courses.AnyAsync(c => c.Id == dto.CourseId.Value, ct);
            if (!courseExists)
            {
                throw new InvalidOperationException($"Course with ID {dto.CourseId} not found.");
            }
        }

        return await DbOperationRetryHelper.ExecuteWithRetryAsync(async () =>
        {
            await unitOfWork.BeginTransactionAsync(ct);
            try
            {
                var correctionRefName = await ledgerRepository.GenerateCorrectionRefNameAsync(ct);

                var entry = new StudentLedgerEntry
                {
                    Id = Guid.NewGuid(),
                    CorrectionRefName = correctionRefName,
                    Description = dto.Description,
                    StudentId = dto.StudentId,
                    CourseId = dto.CourseId,
                    Amount = dto.Amount,
                    EntryType = dto.EntryType,
                    Status = LedgerEntryStatus.Open,
                    CreatedById = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                context.StudentLedgerEntries.Add(entry);
                await context.SaveChangesAsync(ct);
                await unitOfWork.CommitTransactionAsync(ct);

                return await LoadEntryAndMapToDtoAsync(entry.Id, ct);
            }
            catch
            {
                await unitOfWork.RollbackTransactionAsync(ct);
                throw;
            }
        }, ct);
    }

    public async Task<StudentLedgerEntryDto> ReverseEntryAsync(Guid entryId, string reason, Guid userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Reason is required for reversal.", nameof(reason));
        }

        var originalEntry = await ledgerRepository.GetWithApplicationsAsync(entryId, ct)
            ?? throw new InvalidOperationException($"Ledger entry with ID {entryId} not found.");

        if (originalEntry.Status == LedgerEntryStatus.FullyApplied)
        {
            throw new InvalidOperationException("Cannot reverse a fully applied entry. Reverse the applications first.");
        }

        var appliedAmount = originalEntry.Applications.Sum(a => a.AppliedAmount);
        var remainingAmount = originalEntry.Amount - appliedAmount;

        if (remainingAmount <= 0)
        {
            throw new InvalidOperationException("No remaining amount to reverse.");
        }

        var reverseType = originalEntry.EntryType == LedgerEntryType.Credit
            ? LedgerEntryType.Debit
            : LedgerEntryType.Credit;

        var reversalDescription = $"Reversal of {originalEntry.CorrectionRefName}: {reason}";

        // HIGH-1: Validate generated description length
        if (reversalDescription.Length > MaxDescriptionLength)
        {
            reversalDescription = reversalDescription[..MaxDescriptionLength];
        }

        // CRITICAL-1: Use transaction with retry logic
        return await DbOperationRetryHelper.ExecuteWithRetryAsync(async () =>
        {
            await unitOfWork.BeginTransactionAsync(ct);
            try
            {
                var correctionRefName = await ledgerRepository.GenerateCorrectionRefNameAsync(ct);

                var reversalEntry = new StudentLedgerEntry
                {
                    Id = Guid.NewGuid(),
                    CorrectionRefName = correctionRefName,
                    Description = reversalDescription,
                    StudentId = originalEntry.StudentId,
                    CourseId = originalEntry.CourseId,
                    Amount = remainingAmount,
                    EntryType = reverseType,
                    Status = LedgerEntryStatus.FullyApplied,
                    CreatedById = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // HIGH-2: Fix dead code - set correct status based on applied amount
                originalEntry.Status = LedgerEntryStatus.FullyApplied;

                context.StudentLedgerEntries.Add(reversalEntry);
                await context.SaveChangesAsync(ct);
                await unitOfWork.CommitTransactionAsync(ct);

                return await LoadEntryAndMapToDtoAsync(reversalEntry.Id, ct);
            }
            catch
            {
                await unitOfWork.RollbackTransactionAsync(ct);
                throw;
            }
        }, ct);
    }

    public async Task<IReadOnlyList<StudentLedgerEntryDto>> GetStudentLedgerAsync(Guid studentId, CancellationToken ct = default)
    {
        var entries = await ledgerRepository.GetByStudentAsync(studentId, ct);

        // HIGH-3: Fix N+1 - pre-load all CourseTypes needed
        var courseTypeIds = entries
            .Where(e => e.Course != null)
            .Select(e => e.Course!.CourseTypeId)
            .Distinct()
            .ToList();

        var courseTypes = courseTypeIds.Count > 0
            ? await context.CourseTypes
                .Where(ct => courseTypeIds.Contains(ct.Id))
                .ToDictionaryAsync(ct => ct.Id, ct => ct.Name, ct)
            : new Dictionary<Guid, string>();

        return entries.Select(entry => MapToDtoSync(entry, courseTypes)).ToList();
    }

    public async Task<StudentLedgerEntryDto?> GetEntryAsync(Guid entryId, CancellationToken ct = default)
    {
        var entry = await ledgerRepository.GetWithApplicationsAsync(entryId, ct);
        if (entry == null) return null;

        string? courseName = null;
        if (entry.Course != null)
        {
            var courseType = await context.CourseTypes
                .FirstOrDefaultAsync(c => c.Id == entry.Course.CourseTypeId, ct);
            courseName = courseType?.Name;
        }

        return MapToDtoSync(entry, entry.Course != null && courseName != null
            ? new Dictionary<Guid, string> { { entry.Course.CourseTypeId, courseName } }
            : new Dictionary<Guid, string>());
    }

    public async Task<StudentLedgerSummaryDto> GetStudentLedgerSummaryAsync(Guid studentId, CancellationToken ct = default)
    {
        var student = await context.Students.FindAsync([studentId], ct)
            ?? throw new InvalidOperationException($"Student with ID {studentId} not found.");

        var entries = await context.StudentLedgerEntries
            .Where(e => e.StudentId == studentId)
            .Include(e => e.Applications)
            .ToListAsync(ct);

        var totalCredits = entries
            .Where(e => e.EntryType == LedgerEntryType.Credit)
            .Sum(e => e.Amount);

        var totalDebits = entries
            .Where(e => e.EntryType == LedgerEntryType.Debit)
            .Sum(e => e.Amount);

        var availableCredit = await ledgerRepository.GetAvailableCreditAsync(studentId, ct);

        var openEntryCount = entries
            .Count(e => e.Status == LedgerEntryStatus.Open || e.Status == LedgerEntryStatus.PartiallyApplied);

        return new StudentLedgerSummaryDto
        {
            StudentId = studentId,
            StudentName = student.FullName,
            TotalCredits = totalCredits,
            TotalDebits = totalDebits,
            AvailableCredit = availableCredit,
            OpenEntryCount = openEntryCount
        };
    }

    public async Task<ApplyCreditResultDto> ApplyCreditsToInvoiceAsync(Guid invoiceId, Guid userId, CancellationToken ct = default)
    {
        // CRITICAL-3: Wrap entire operation in transaction
        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var invoice = await context.Invoices
                .Include(i => i.Payments)
                .Include(i => i.LedgerApplications)
                .FirstOrDefaultAsync(i => i.Id == invoiceId, ct)
                ?? throw new InvalidOperationException($"Invoice with ID {invoiceId} not found.");

            var paidAmount = invoice.Payments.Sum(p => p.Amount) + invoice.LedgerApplications.Sum(a => a.AppliedAmount);
            var outstandingBalance = invoice.Total - paidAmount;

            if (outstandingBalance <= 0)
            {
                await unitOfWork.RollbackTransactionAsync(ct);
                return new ApplyCreditResultDto
                {
                    InvoiceId = invoice.Id,
                    InvoiceNumber = invoice.InvoiceNumber,
                    AmountApplied = 0,
                    RemainingBalance = 0,
                    Applications = []
                };
            }

            // CRITICAL-4: Credits are fetched using invoice.StudentId, ensuring ownership
            var openCredits = await ledgerRepository.GetOpenEntriesForStudentAsync(invoice.StudentId, ct);
            var applications = new List<LedgerApplicationDto>();
            var totalApplied = 0m;

            // HIGH-4: Move user lookup outside the loop
            var appliedByUser = await context.Users.FindAsync([userId], ct);
            var appliedByName = appliedByUser != null
                ? $"{appliedByUser.FirstName} {appliedByUser.LastName}".Trim()
                : UnknownUserName;
            if (string.IsNullOrWhiteSpace(appliedByName))
            {
                appliedByName = appliedByUser?.Email ?? UnknownUserName;
            }

            foreach (var credit in openCredits)
            {
                if (outstandingBalance <= 0)
                    break;

                var appliedAmount = credit.Applications.Sum(a => a.AppliedAmount);
                var availableAmount = credit.Amount - appliedAmount;

                if (availableAmount <= 0)
                    continue;

                var amountToApply = Math.Min(availableAmount, outstandingBalance);

                var application = new StudentLedgerApplication
                {
                    Id = Guid.NewGuid(),
                    LedgerEntryId = credit.Id,
                    InvoiceId = invoice.Id,
                    AppliedAmount = amountToApply,
                    AppliedById = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                context.StudentLedgerApplications.Add(application);

                var newAppliedAmount = appliedAmount + amountToApply;
                credit.Status = newAppliedAmount >= credit.Amount
                    ? LedgerEntryStatus.FullyApplied
                    : LedgerEntryStatus.PartiallyApplied;

                outstandingBalance -= amountToApply;
                totalApplied += amountToApply;

                applications.Add(new LedgerApplicationDto
                {
                    Id = application.Id,
                    InvoiceId = invoice.Id,
                    InvoiceNumber = invoice.InvoiceNumber,
                    AppliedAmount = amountToApply,
                    AppliedAt = application.CreatedAt,
                    AppliedByName = appliedByName
                });
            }

            if (outstandingBalance <= 0 && invoice.Status != InvoiceStatus.Paid)
            {
                invoice.Status = InvoiceStatus.Paid;
                invoice.PaidAt = DateTime.UtcNow;
            }

            await context.SaveChangesAsync(ct);
            await unitOfWork.CommitTransactionAsync(ct);

            return new ApplyCreditResultDto
            {
                InvoiceId = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                AmountApplied = totalApplied,
                RemainingBalance = Math.Max(0, outstandingBalance),
                Applications = applications
            };
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }
    }

    public async Task<DecoupleApplicationResultDto> DecoupleApplicationAsync(Guid applicationId, string reason, Guid userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Reason is required for decoupling.", nameof(reason));
        }

        var application = await context.StudentLedgerApplications
            .Include(a => a.LedgerEntry)
                .ThenInclude(e => e.Applications)
            .Include(a => a.Invoice)
                .ThenInclude(i => i.Payments)
            .Include(a => a.Invoice)
                .ThenInclude(i => i.LedgerApplications)
            .FirstOrDefaultAsync(a => a.Id == applicationId, ct)
            ?? throw new InvalidOperationException($"Ledger application with ID {applicationId} not found.");

        return await DbOperationRetryHelper.ExecuteWithRetryAsync(async () =>
        {
            await unitOfWork.BeginTransactionAsync(ct);
            try
            {
                var entry = application.LedgerEntry;
                var invoice = application.Invoice;
                var decoupledAmount = application.AppliedAmount;

                context.StudentLedgerApplications.Remove(application);

                // Recalculate entry status from remaining applications
                var remainingApplied = entry.Applications
                    .Where(a => a.Id != applicationId)
                    .Sum(a => a.AppliedAmount);

                entry.Status = remainingApplied <= 0
                    ? LedgerEntryStatus.Open
                    : remainingApplied < entry.Amount
                        ? LedgerEntryStatus.PartiallyApplied
                        : LedgerEntryStatus.FullyApplied;

                // If invoice was Paid, check whether it should revert
                if (invoice.Status == InvoiceStatus.Paid)
                {
                    var totalPaid = invoice.Payments.Sum(p => p.Amount)
                        + invoice.LedgerApplications
                            .Where(a => a.Id != applicationId)
                            .Sum(a => a.AppliedAmount);

                    if (totalPaid < invoice.Total)
                    {
                        invoice.Status = invoice.DueDate < DateOnly.FromDateTime(DateTime.UtcNow)
                            ? InvoiceStatus.Overdue
                            : InvoiceStatus.Sent;
                        invoice.PaidAt = null;
                    }
                }

                await context.SaveChangesAsync(ct);
                await unitOfWork.CommitTransactionAsync(ct);

                var user = await context.Users.FindAsync([userId], ct);
                var userName = user != null
                    ? $"{user.FirstName} {user.LastName}".Trim()
                    : UnknownUserName;
                if (string.IsNullOrWhiteSpace(userName))
                {
                    userName = user?.Email ?? UnknownUserName;
                }

                return new DecoupleApplicationResultDto
                {
                    LedgerEntryId = entry.Id,
                    CorrectionRefName = entry.CorrectionRefName,
                    InvoiceId = invoice.Id,
                    InvoiceNumber = invoice.InvoiceNumber,
                    DecoupledAmount = decoupledAmount,
                    NewEntryStatus = entry.Status,
                    NewInvoiceStatus = invoice.Status,
                    DecoupledAt = DateTime.UtcNow,
                    DecoupledByName = userName
                };
            }
            catch
            {
                await unitOfWork.RollbackTransactionAsync(ct);
                throw;
            }
        }, ct);
    }

    public async Task<decimal> GetAvailableCreditForStudentAsync(Guid studentId, CancellationToken ct = default)
    {
        return await ledgerRepository.GetAvailableCreditAsync(studentId, ct);
    }

    public async Task<string> GenerateCorrectionRefNameAsync(CancellationToken ct = default)
    {
        return await ledgerRepository.GenerateCorrectionRefNameAsync(ct);
    }

    private async Task<StudentLedgerEntryDto> LoadEntryAndMapToDtoAsync(Guid entryId, CancellationToken ct)
    {
        var savedEntry = await context.StudentLedgerEntries
            .Include(e => e.Course)
            .Include(e => e.Student)
            .Include(e => e.CreatedBy)
            .Include(e => e.Applications)
                .ThenInclude(a => a.Invoice)
            .Include(e => e.Applications)
                .ThenInclude(a => a.AppliedBy)
            .FirstAsync(e => e.Id == entryId, ct);

        var courseTypes = new Dictionary<Guid, string>();
        if (savedEntry.Course != null)
        {
            var courseType = await context.CourseTypes
                .FirstOrDefaultAsync(c => c.Id == savedEntry.Course.CourseTypeId, ct);
            if (courseType != null)
            {
                courseTypes[savedEntry.Course.CourseTypeId] = courseType.Name;
            }
        }

        return MapToDtoSync(savedEntry, courseTypes);
    }

    private static StudentLedgerEntryDto MapToDtoSync(StudentLedgerEntry entry, Dictionary<Guid, string> courseTypes)
    {
        var appliedAmount = entry.Applications.Sum(a => a.AppliedAmount);

        string? courseName = null;
        if (entry.Course != null && courseTypes.TryGetValue(entry.Course.CourseTypeId, out var name))
        {
            courseName = name;
        }

        var createdByName = UnknownUserName;
        if (entry.CreatedBy != null)
        {
            createdByName = $"{entry.CreatedBy.FirstName} {entry.CreatedBy.LastName}".Trim();
            if (string.IsNullOrWhiteSpace(createdByName))
            {
                createdByName = entry.CreatedBy.Email ?? UnknownUserName;
            }
        }

        var applications = entry.Applications.Select(a => new LedgerApplicationDto
        {
            Id = a.Id,
            InvoiceId = a.InvoiceId,
            InvoiceNumber = a.Invoice?.InvoiceNumber ?? string.Empty,
            AppliedAmount = a.AppliedAmount,
            AppliedAt = a.CreatedAt,
            AppliedByName = a.AppliedBy != null
                ? $"{a.AppliedBy.FirstName} {a.AppliedBy.LastName}".Trim()
                : UnknownUserName
        }).ToList();

        return new StudentLedgerEntryDto
        {
            Id = entry.Id,
            CorrectionRefName = entry.CorrectionRefName,
            Description = entry.Description,
            StudentId = entry.StudentId,
            StudentName = entry.Student?.FullName ?? string.Empty,
            CourseId = entry.CourseId,
            CourseName = courseName,
            Amount = entry.Amount,
            EntryType = entry.EntryType,
            Status = entry.Status,
            AppliedAmount = appliedAmount,
            RemainingAmount = entry.Amount - appliedAmount,
            CreatedAt = entry.CreatedAt,
            CreatedByName = createdByName,
            Applications = applications
        };
    }

    [Obsolete("Use MapToDtoSync instead to avoid N+1 queries")]
    private async Task<StudentLedgerEntryDto> MapToDto(StudentLedgerEntry entry, CancellationToken cancellationToken)
    {
        string? courseName = null;
        if (entry.Course != null)
        {
            var courseType = await context.CourseTypes
                .FirstOrDefaultAsync(c => c.Id == entry.Course.CourseTypeId, cancellationToken);
            courseName = courseType?.Name;
        }

        return MapToDtoSync(entry, entry.Course != null && courseName != null
            ? new Dictionary<Guid, string> { { entry.Course.CourseTypeId, courseName } }
            : new Dictionary<Guid, string>());
    }
}
