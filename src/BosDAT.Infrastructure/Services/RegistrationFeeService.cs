using Microsoft.EntityFrameworkCore;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;
using BosDAT.Infrastructure.Data;

namespace BosDAT.Infrastructure.Services;

public class RegistrationFeeService(
    ApplicationDbContext context,
    IStudentLedgerRepository ledgerRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService) : IRegistrationFeeService
{
    private const int MaxRetries = 3;

    public async Task<bool> IsStudentEligibleForFeeAsync(Guid studentId, CancellationToken ct = default)
    {
        var student = await context.Students
            .FirstOrDefaultAsync(s => s.Id == studentId, ct);

        if (student == null)
        {
            throw new InvalidOperationException($"Student with ID {studentId} not found.");
        }

        return student.RegistrationFeePaidAt == null;
    }

    public async Task<bool> ShouldApplyFeeForCourseAsync(Guid courseId, CancellationToken ct = default)
    {
        var course = await context.Courses
            .FirstOrDefaultAsync(c => c.Id == courseId, ct);

        if (course == null)
        {
            throw new InvalidOperationException($"Course with ID {courseId} not found.");
        }

        return !course.IsTrial;
    }

    public async Task<Guid> ApplyRegistrationFeeAsync(Guid studentId, CancellationToken ct = default)
    {
        var userId = currentUserService.UserId
            ?? throw new InvalidOperationException("No authenticated user found. Cannot apply registration fee.");

        var student = await context.Students
            .FirstOrDefaultAsync(s => s.Id == studentId, ct);

        if (student == null)
        {
            throw new InvalidOperationException($"Student with ID {studentId} not found.");
        }

        if (student.RegistrationFeePaidAt != null)
        {
            throw new InvalidOperationException($"Registration fee has already been applied for student {studentId}.");
        }

        var feeAmount = await GetFeeAmountAsync(ct);
        var feeDescription = await GetFeeDescriptionAsync(ct);

        return await ExecuteWithRetryAsync(async () =>
        {
            await unitOfWork.BeginTransactionAsync(ct);
            try
            {
                var correctionRefName = await ledgerRepository.GenerateCorrectionRefNameAsync(ct);

                var ledgerEntry = new StudentLedgerEntry
                {
                    Id = Guid.NewGuid(),
                    CorrectionRefName = correctionRefName,
                    Description = feeDescription,
                    StudentId = studentId,
                    CourseId = null,
                    Amount = feeAmount,
                    EntryType = LedgerEntryType.Debit,
                    Status = LedgerEntryStatus.Open,
                    CreatedById = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                context.StudentLedgerEntries.Add(ledgerEntry);

                student.RegistrationFeePaidAt = DateTime.UtcNow;

                await context.SaveChangesAsync(ct);
                await unitOfWork.CommitTransactionAsync(ct);

                return ledgerEntry.Id;
            }
            catch
            {
                await unitOfWork.RollbackTransactionAsync(ct);
                throw;
            }
        }, ct);
    }

    public async Task<RegistrationFeeStatusDto> GetFeeStatusAsync(Guid studentId, CancellationToken ct = default)
    {
        var student = await context.Students
            .FirstOrDefaultAsync(s => s.Id == studentId, ct);

        if (student == null)
        {
            throw new InvalidOperationException($"Student with ID {studentId} not found.");
        }

        if (student.RegistrationFeePaidAt == null)
        {
            var feeAmount = await GetFeeAmountAsync(ct);
            return new RegistrationFeeStatusDto
            {
                HasPaid = false,
                PaidAt = null,
                Amount = feeAmount,
                LedgerEntryId = null
            };
        }

        var feeDescription = await GetFeeDescriptionAsync(ct);
        var ledgerEntry = await context.StudentLedgerEntries
            .Where(e => e.StudentId == studentId &&
                        e.Description == feeDescription &&
                        e.EntryType == LedgerEntryType.Debit)
            .OrderByDescending(e => e.CreatedAt)
            .FirstOrDefaultAsync(ct);

        return new RegistrationFeeStatusDto
        {
            HasPaid = true,
            PaidAt = student.RegistrationFeePaidAt,
            Amount = ledgerEntry?.Amount,
            LedgerEntryId = ledgerEntry?.Id
        };
    }

    private async Task<decimal> GetFeeAmountAsync(CancellationToken ct)
    {
        var setting = await context.Settings
            .FirstOrDefaultAsync(s => s.Key == "registration_fee", ct);

        if (setting == null || !decimal.TryParse(setting.Value, out var amount))
        {
            return 25m;
        }

        return amount;
    }

    private async Task<string> GetFeeDescriptionAsync(CancellationToken ct)
    {
        var setting = await context.Settings
            .FirstOrDefaultAsync(s => s.Key == "registration_fee_description", ct);

        return setting?.Value ?? "Eenmalig inschrijfgeld";
    }

    private static async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, CancellationToken ct)
    {
        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (DbUpdateException ex) when (attempt < MaxRetries && IsDuplicateKeyException(ex))
            {
                await Task.Delay(50 * attempt, ct);
            }
        }

        return await operation();
    }

    private static bool IsDuplicateKeyException(DbUpdateException ex)
    {
        return ex.InnerException?.Message.Contains("duplicate key") == true ||
               ex.InnerException?.Message.Contains("unique constraint") == true;
    }
}
