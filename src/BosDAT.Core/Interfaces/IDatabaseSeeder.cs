namespace BosDAT.Core.Interfaces;

/// <summary>
/// Service for seeding the database with comprehensive test data
/// </summary>
public interface IDatabaseSeeder
{
    /// <summary>
    /// Seeds the database with comprehensive test data including:
    /// - Teachers for all instruments
    /// - Course types for all instruments (Individual, Group, Workshop)
    /// - Courses with various recurrences (trial, weekly, bi-weekly)
    /// - Students with enrollments
    /// - Historic lessons with invoices
    /// - Registration fees
    /// - Open corrections (ledger entries)
    /// </summary>
    Task SeedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets the database by removing all seeded data while preserving:
    /// - Admin user (admin@bosdat.nl)
    /// - Global settings
    /// - Reference data (Instruments, Rooms)
    /// </summary>
    Task ResetAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the database has been seeded with test data
    /// </summary>
    Task<bool> IsSeededAsync(CancellationToken cancellationToken = default);
}
