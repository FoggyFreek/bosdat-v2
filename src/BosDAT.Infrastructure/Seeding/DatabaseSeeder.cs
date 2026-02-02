using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;
using BosDAT.Infrastructure.Data;
using BosDAT.Infrastructure.Seeding.DataGenerators;

namespace BosDAT.Infrastructure.Seeding;

/// <summary>
/// Comprehensive database seeder for BosDAT music school management system.
/// Orchestrates data generation across all entity types with proper relationships.
/// </summary>
public class DatabaseSeeder : IDatabaseSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<bool> IsSeededAsync(CancellationToken cancellationToken = default)
    {
        var teacherCount = await _context.Teachers.CountAsync(cancellationToken);
        return teacherCount > 1;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting database seeding...");

        try
        {
            // Initialize seeding context
            var seederContext = new SeederContext();

            // Validate prerequisites
            var adminUser = await ValidatePrerequisitesAsync(seederContext, cancellationToken);

            // Create data generators
            var teacherGenerator = new TeacherDataGenerator(_context, seederContext);
            var studentGenerator = new StudentDataGenerator(_context, seederContext);
            var courseGenerator = new CourseDataGenerator(_context, seederContext);
            var lessonGenerator = new LessonDataGenerator(_context, seederContext);
            var invoiceGenerator = new InvoiceDataGenerator(_context, seederContext);
            var supportGenerator = new SupportDataGenerator(_context, seederContext);

            // 1. Seed Teachers
            _logger.LogInformation("Seeding teachers...");
            var teachers = await teacherGenerator.GenerateAsync(cancellationToken);

            // 2. Seed CourseTypes for all instruments
            _logger.LogInformation("Seeding course types...");
            var courseTypes = await courseGenerator.GenerateCourseTypesAsync(
                seederContext.Instruments, cancellationToken);

            // 3. Seed CourseTypePricingVersions
            _logger.LogInformation("Seeding pricing versions...");
            var pricingVersions = await courseGenerator.GeneratePricingVersionsAsync(
                courseTypes, cancellationToken);

            // 4. Link teachers to course types
            _logger.LogInformation("Linking teachers to course types...");
            await teacherGenerator.GenerateCourseTypeLinksAsync(
                teachers, courseTypes, cancellationToken);

            // 5. Seed Students
            _logger.LogInformation("Seeding students...");
            var students = await studentGenerator.GenerateAsync(cancellationToken);

            // 6. Seed Courses with various recurrences
            _logger.LogInformation("Seeding courses...");
            var courses = await courseGenerator.GenerateCoursesAsync(
                teachers, courseTypes, seederContext.Rooms, cancellationToken);

            // 7. Seed Enrollments
            _logger.LogInformation("Seeding enrollments...");
            var enrollments = await courseGenerator.GenerateEnrollmentsAsync(
                students, courses, cancellationToken);

            // 8. Seed Lessons (historic)
            _logger.LogInformation("Seeding lessons...");
            var lessons = await lessonGenerator.GenerateAsync(
                courses, enrollments, cancellationToken);

            // 9. Seed Invoices with lines
            _logger.LogInformation("Seeding invoices...");
            var invoices = await invoiceGenerator.GenerateAsync(
                students, lessons, pricingVersions, cancellationToken);

            // 10. Seed Payments
            _logger.LogInformation("Seeding payments...");
            await supportGenerator.GeneratePaymentsAsync(invoices, cancellationToken);

            // 11. Seed Ledger Entries (open corrections)
            _logger.LogInformation("Seeding ledger entries...");
            await supportGenerator.GenerateLedgerEntriesAsync(
                students, courses, adminUser.Id, cancellationToken);

            // 12. Seed Holidays
            _logger.LogInformation("Seeding holidays...");
            await supportGenerator.GenerateHolidaysAsync(cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Database seeding completed successfully!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during database seeding");
            throw;
        }
    }

    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting database reset...");

        try
        {
            // Delete in reverse dependency order to avoid FK violations
            // Preserves: Admin user, Settings, Instruments, Rooms

            await DeleteInOrderAsync(cancellationToken);

            _logger.LogInformation(
                "Database reset completed. Admin user, settings, instruments, and rooms preserved.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during database reset");
            throw;
        }
    }

    private async Task<ApplicationUser> ValidatePrerequisitesAsync(
        SeederContext seederContext,
        CancellationToken cancellationToken)
    {
        // Get admin user for ledger entries
        var adminUser = await _userManager.FindByEmailAsync(SeederConstants.AdminEmail);
        if (adminUser == null)
        {
            throw new InvalidOperationException(
                $"Admin user '{SeederConstants.AdminEmail}' not found. " +
                "Please ensure the admin user exists before seeding.");
        }
        seederContext.AdminUserId = adminUser.Id;

        // Get all instruments
        var instruments = await _context.Instruments.ToListAsync(cancellationToken);
        if (!instruments.Any())
        {
            throw new InvalidOperationException(
                "No instruments found. Please run migrations first.");
        }
        seederContext.Instruments = instruments;

        // Get all rooms
        seederContext.Rooms = await _context.Rooms.ToListAsync(cancellationToken);

        return adminUser;
    }

    private async Task DeleteInOrderAsync(CancellationToken cancellationToken)
    {
        // Order matters - delete child tables before parent tables

        // Financial data
        await _context.StudentLedgerApplications.ExecuteDeleteAsync(cancellationToken);
        await _context.StudentLedgerEntries.ExecuteDeleteAsync(cancellationToken);
        await _context.Payments.ExecuteDeleteAsync(cancellationToken);
        await _context.InvoiceLines.ExecuteDeleteAsync(cancellationToken);
        await _context.Invoices.ExecuteDeleteAsync(cancellationToken);

        // Lesson data
        await _context.Lessons.ExecuteDeleteAsync(cancellationToken);

        // Enrollment data
        await _context.Enrollments.ExecuteDeleteAsync(cancellationToken);
        await _context.Cancellations.ExecuteDeleteAsync(cancellationToken);

        // Course data
        await _context.Courses.ExecuteDeleteAsync(cancellationToken);
        await _context.TeacherCourseTypes.ExecuteDeleteAsync(cancellationToken);
        await _context.CourseTypePricingVersions.ExecuteDeleteAsync(cancellationToken);
        await _context.CourseTypes.ExecuteDeleteAsync(cancellationToken);

        // Teacher data
        await _context.TeacherInstruments.ExecuteDeleteAsync(cancellationToken);
        await _context.TeacherPayments.ExecuteDeleteAsync(cancellationToken);
        await _context.Teachers.ExecuteDeleteAsync(cancellationToken);

        // Student data
        await _context.Students.ExecuteDeleteAsync(cancellationToken);

        // Reference data (holidays only - keep instruments/rooms)
        await _context.Holidays.ExecuteDeleteAsync(cancellationToken);
    }
}
