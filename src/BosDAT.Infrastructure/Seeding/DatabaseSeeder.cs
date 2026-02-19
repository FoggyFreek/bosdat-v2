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
            await ValidatePrerequisitesAsync(seederContext, cancellationToken);

            // Create data generators
            var teacherGenerator = new TeacherDataGenerator(_context, seederContext);
            var teacherAvailabilityGenerator = new TeacherAvailabilityDataGenerator(_context);
            var studentGenerator = new StudentDataGenerator(_context, seederContext);
            var courseGenerator = new CourseDataGenerator(_context, seederContext);
            var lessonGenerator = new LessonDataGenerator(_context, seederContext);
            var invoiceGenerator = new InvoiceDataGenerator(_context, seederContext);
            var supportGenerator = new SupportDataGenerator(_context, seederContext);

            // 1. Seed Teachers
            _logger.LogInformation("Seeding teachers...");
            var teachers = await teacherGenerator.GenerateAsync(cancellationToken);

            // 1b. Seed Teacher Availability (default 09:00-22:00 for all days)
            _logger.LogInformation("Seeding teacher availability...");
            await teacherAvailabilityGenerator.GenerateAsync(teachers, cancellationToken);

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

            // 11. Seed Holidays
            _logger.LogInformation("Seeding holidays...");
            await supportGenerator.GenerateHolidaysAsync(cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Database seeding completed successfully!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during database seeding");
            throw new InvalidOperationException("Database seeding failed. See inner exception for details.", ex);
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
            throw new InvalidOperationException("Database reset failed. See inner exception for details.", ex);
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
        if (instruments.Count == 0)
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
        // Using RemoveRange instead of ExecuteDelete for in-memory database compatibility

        // Financial data
        var payments = await _context.Payments.ToListAsync(cancellationToken);
        _context.Payments.RemoveRange(payments);

        var invoiceLines = await _context.InvoiceLines.ToListAsync(cancellationToken);
        _context.InvoiceLines.RemoveRange(invoiceLines);

        var invoices = await _context.Invoices.ToListAsync(cancellationToken);
        _context.Invoices.RemoveRange(invoices);

        // Lesson data
        var lessons = await _context.Lessons.ToListAsync(cancellationToken);
        _context.Lessons.RemoveRange(lessons);

        // Enrollment data
        var enrollments = await _context.Enrollments.ToListAsync(cancellationToken);
        _context.Enrollments.RemoveRange(enrollments);

        var cancellations = await _context.Cancellations.ToListAsync(cancellationToken);
        _context.Cancellations.RemoveRange(cancellations);

        // Course data
        var courses = await _context.Courses.ToListAsync(cancellationToken);
        _context.Courses.RemoveRange(courses);

        var teacherCourseTypes = await _context.TeacherCourseTypes.ToListAsync(cancellationToken);
        _context.TeacherCourseTypes.RemoveRange(teacherCourseTypes);

        var pricingVersions = await _context.CourseTypePricingVersions.ToListAsync(cancellationToken);
        _context.CourseTypePricingVersions.RemoveRange(pricingVersions);

        var courseTypes = await _context.CourseTypes.ToListAsync(cancellationToken);
        _context.CourseTypes.RemoveRange(courseTypes);

        // Teacher data
        var teacherAvailability = await _context.TeacherAvailabilities.ToListAsync(cancellationToken);
        _context.TeacherAvailabilities.RemoveRange(teacherAvailability);

        var teacherInstruments = await _context.TeacherInstruments.ToListAsync(cancellationToken);
        _context.TeacherInstruments.RemoveRange(teacherInstruments);

        var teacherPayments = await _context.TeacherPayments.ToListAsync(cancellationToken);
        _context.TeacherPayments.RemoveRange(teacherPayments);

        var teachers = await _context.Teachers.ToListAsync(cancellationToken);
        _context.Teachers.RemoveRange(teachers);

        // Student data
        var students = await _context.Students.ToListAsync(cancellationToken);
        _context.Students.RemoveRange(students);

        // Reference data (holidays only - keep instruments/rooms)
        var holidays = await _context.Holidays.ToListAsync(cancellationToken);
        _context.Holidays.RemoveRange(holidays);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
