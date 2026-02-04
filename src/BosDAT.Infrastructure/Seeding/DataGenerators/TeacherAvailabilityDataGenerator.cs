using Microsoft.EntityFrameworkCore;
using BosDAT.Core.Entities;
using BosDAT.Infrastructure.Data;

namespace BosDAT.Infrastructure.Seeding.DataGenerators;

/// <summary>
/// Generates default teacher availability data.
/// Creates 7 entries per teacher (one per day) with default 09:00-22:00 availability.
/// </summary>
public class TeacherAvailabilityDataGenerator
{
    private readonly ApplicationDbContext _context;

    private static readonly TimeOnly DefaultFromTime = new(9, 0);
    private static readonly TimeOnly DefaultUntilTime = new(22, 0);

    public TeacherAvailabilityDataGenerator(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task GenerateAsync(List<Teacher> teachers, CancellationToken cancellationToken)
    {
        var existingCount = await _context.TeacherAvailabilities.CountAsync(cancellationToken);
        if (existingCount > 0)
        {
            return;
        }

        var availabilityEntries = new List<TeacherAvailability>();
        var createdAt = DateTime.UtcNow;

        foreach (var teacher in teachers)
        {
            // Create availability for all 7 days of the week
            for (var day = DayOfWeek.Sunday; day <= DayOfWeek.Saturday; day++)
            {
                availabilityEntries.Add(new TeacherAvailability
                {
                    Id = Guid.NewGuid(),
                    TeacherId = teacher.Id,
                    DayOfWeek = day,
                    FromTime = DefaultFromTime,
                    UntilTime = DefaultUntilTime,
                    CreatedAt = createdAt,
                    UpdatedAt = createdAt
                });
            }
        }

        await _context.TeacherAvailabilities.AddRangeAsync(availabilityEntries, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
