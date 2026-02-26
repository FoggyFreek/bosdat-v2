using Microsoft.EntityFrameworkCore;
using BosDAT.Core.DTOs;
using BosDAT.Core.Interfaces;
using BosDAT.Infrastructure.Data;

namespace BosDAT.Infrastructure.Services;

public class RegistrationFeeService(
    ApplicationDbContext context) : IRegistrationFeeService
{
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
            };
        }

        //todo
        return new RegistrationFeeStatusDto
        {
            HasPaid = true,
            PaidAt = student.RegistrationFeePaidAt,
        };
    }

    public async Task<decimal> GetFeeAmountAsync(CancellationToken ct = default)
    {
        var setting = await context.Settings
            .FirstOrDefaultAsync(s => s.Key == "registration_fee", ct);

        if (setting == null || !decimal.TryParse(setting.Value, out var amount))
        {
            return 25m;
        }

        return amount;
    }

    public async Task<string> GetFeeDescriptionAsync(CancellationToken ct = default)
    {
        var setting = await context.Settings
            .FirstOrDefaultAsync(s => s.Key == "registration_fee_description", ct);

        return setting?.Value ?? "Eenmalig inschrijfgeld";
    }
}
