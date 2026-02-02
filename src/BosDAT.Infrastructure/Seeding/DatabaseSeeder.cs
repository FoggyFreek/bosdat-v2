using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Core.Interfaces;
using BosDAT.Infrastructure.Data;

namespace BosDAT.Infrastructure.Seeding;

/// <summary>
/// Comprehensive database seeder for BosDAT music school management system.
/// Creates realistic test data for all entity types with proper relationships.
/// </summary>
public class DatabaseSeeder : IDatabaseSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<DatabaseSeeder> _logger;

    // Constants for seeding
    private const string AdminEmail = "admin@bosdat.nl";
    private const decimal VatRate = 21m;
    private const decimal RegistrationFee = 25m;
    private const int ChildAgeLimit = 18;

    // Pre-generated GUIDs for consistent seeding
    private static readonly Guid[] TeacherIds =
    {
        Guid.Parse("10000001-0001-0001-0001-000000000001"),
        Guid.Parse("10000001-0001-0001-0001-000000000002"),
        Guid.Parse("10000001-0001-0001-0001-000000000003"),
        Guid.Parse("10000001-0001-0001-0001-000000000004"),
        Guid.Parse("10000001-0001-0001-0001-000000000005"),
        Guid.Parse("10000001-0001-0001-0001-000000000006"),
        Guid.Parse("10000001-0001-0001-0001-000000000007"),
        Guid.Parse("10000001-0001-0001-0001-000000000008")
    };

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
        // Check if we have seeded teachers (beyond initial test data)
        var teacherCount = await _context.Teachers.CountAsync(cancellationToken);
        return teacherCount > 1;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting database seeding...");

        try
        {
            // Get admin user for ledger entries
            var adminUser = await _userManager.FindByEmailAsync(AdminEmail);
            if (adminUser == null)
            {
                throw new InvalidOperationException($"Admin user '{AdminEmail}' not found. Please ensure the admin user exists before seeding.");
            }

            // Get all instruments
            var instruments = await _context.Instruments.ToListAsync(cancellationToken);
            if (!instruments.Any())
            {
                throw new InvalidOperationException("No instruments found. Please run migrations first.");
            }

            // Get all rooms
            var rooms = await _context.Rooms.ToListAsync(cancellationToken);

            // 1. Seed Teachers
            _logger.LogInformation("Seeding teachers...");
            var teachers = await SeedTeachersAsync(instruments, cancellationToken);

            // 2. Seed CourseTypes for all instruments
            _logger.LogInformation("Seeding course types...");
            var courseTypes = await SeedCourseTypesAsync(instruments, cancellationToken);

            // 3. Seed CourseTypePricingVersions
            _logger.LogInformation("Seeding pricing versions...");
            var pricingVersions = await SeedPricingVersionsAsync(courseTypes, cancellationToken);

            // 4. Link teachers to course types
            _logger.LogInformation("Linking teachers to course types...");
            await SeedTeacherCourseTypesAsync(teachers, courseTypes, cancellationToken);

            // 5. Seed Students
            _logger.LogInformation("Seeding students...");
            var students = await SeedStudentsAsync(cancellationToken);

            // 6. Seed Courses with various recurrences
            _logger.LogInformation("Seeding courses...");
            var courses = await SeedCoursesAsync(teachers, courseTypes, rooms, cancellationToken);

            // 7. Seed Enrollments
            _logger.LogInformation("Seeding enrollments...");
            var enrollments = await SeedEnrollmentsAsync(students, courses, cancellationToken);

            // 8. Seed Lessons (historic)
            _logger.LogInformation("Seeding lessons...");
            var lessons = await SeedLessonsAsync(courses, enrollments, cancellationToken);

            // 9. Seed Invoices with lines
            _logger.LogInformation("Seeding invoices...");
            var invoices = await SeedInvoicesAsync(students, lessons, pricingVersions, cancellationToken);

            // 10. Seed some payments
            _logger.LogInformation("Seeding payments...");
            await SeedPaymentsAsync(invoices, cancellationToken);

            // 11. Seed Ledger Entries (open corrections)
            _logger.LogInformation("Seeding ledger entries...");
            await SeedLedgerEntriesAsync(students, courses, adminUser.Id, cancellationToken);

            // 12. Seed Holidays
            _logger.LogInformation("Seeding holidays...");
            await SeedHolidaysAsync(cancellationToken);

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
            // Keep: Admin user, Settings, Instruments, Rooms

            // 1. Delete ledger applications first
            await _context.StudentLedgerApplications.ExecuteDeleteAsync(cancellationToken);

            // 2. Delete ledger entries
            await _context.StudentLedgerEntries.ExecuteDeleteAsync(cancellationToken);

            // 3. Delete payments
            await _context.Payments.ExecuteDeleteAsync(cancellationToken);

            // 4. Delete invoice lines
            await _context.InvoiceLines.ExecuteDeleteAsync(cancellationToken);

            // 5. Delete invoices
            await _context.Invoices.ExecuteDeleteAsync(cancellationToken);

            // 6. Delete lessons
            await _context.Lessons.ExecuteDeleteAsync(cancellationToken);

            // 7. Delete enrollments
            await _context.Enrollments.ExecuteDeleteAsync(cancellationToken);

            // 8. Delete cancellations
            await _context.Cancellations.ExecuteDeleteAsync(cancellationToken);

            // 9. Delete courses
            await _context.Courses.ExecuteDeleteAsync(cancellationToken);

            // 10. Delete teacher-course-type mappings
            await _context.TeacherCourseTypes.ExecuteDeleteAsync(cancellationToken);

            // 11. Delete pricing versions
            await _context.CourseTypePricingVersions.ExecuteDeleteAsync(cancellationToken);

            // 12. Delete course types
            await _context.CourseTypes.ExecuteDeleteAsync(cancellationToken);

            // 13. Delete teacher-instrument mappings
            await _context.TeacherInstruments.ExecuteDeleteAsync(cancellationToken);

            // 14. Delete teacher payments
            await _context.TeacherPayments.ExecuteDeleteAsync(cancellationToken);

            // 15. Delete teachers (keep admin if linked)
            await _context.Teachers.ExecuteDeleteAsync(cancellationToken);

            // 16. Delete students
            await _context.Students.ExecuteDeleteAsync(cancellationToken);

            // 17. Delete holidays
            await _context.Holidays.ExecuteDeleteAsync(cancellationToken);

            // 18. Clear audit logs (optional - keep for audit trail)
            // await _context.AuditLogs.ExecuteDeleteAsync(cancellationToken);

            _logger.LogInformation("Database reset completed. Admin user, settings, instruments, and rooms preserved.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during database reset");
            throw;
        }
    }

    #region Seeding Methods

    private async Task<List<Teacher>> SeedTeachersAsync(List<Instrument> instruments, CancellationToken cancellationToken)
    {
        // Check if teachers already exist
        if (await _context.Teachers.AnyAsync(cancellationToken))
        {
            return await _context.Teachers.ToListAsync(cancellationToken);
        }

        var teacherData = new[]
        {
            (FirstName: "Maria", LastName: "van den Berg", Prefix: "", InstrumentIds: new[] { 1, 10 }, HourlyRate: 45m, Role: TeacherRole.Teacher), // Piano, Keyboard
            (FirstName: "Jan", LastName: "de Vries", Prefix: "", InstrumentIds: new[] { 2, 3 }, HourlyRate: 40m, Role: TeacherRole.Teacher), // Guitar, Bass
            (FirstName: "Sophie", LastName: "Jansen", Prefix: "", InstrumentIds: new[] { 5 }, HourlyRate: 50m, Role: TeacherRole.Teacher), // Violin
            (FirstName: "Peter", LastName: "Bakker", Prefix: "", InstrumentIds: new[] { 4 }, HourlyRate: 42m, Role: TeacherRole.Teacher), // Drums
            (FirstName: "Emma", LastName: "Visser", Prefix: "", InstrumentIds: new[] { 6 }, HourlyRate: 48m, Role: TeacherRole.Teacher), // Vocals
            (FirstName: "Thomas", LastName: "de Jong", Prefix: "", InstrumentIds: new[] { 7, 8, 9 }, HourlyRate: 55m, Role: TeacherRole.Teacher), // Sax, Flute, Trumpet
            (FirstName: "Lisa", LastName: "Mulder", Prefix: "", InstrumentIds: new[] { 1, 2 }, HourlyRate: 38m, Role: TeacherRole.Teacher), // Piano, Guitar (junior)
            (FirstName: "David", LastName: "Smit", Prefix: "", InstrumentIds: new[] { 4, 2 }, HourlyRate: 43m, Role: TeacherRole.Staff) // Drums, Guitar (staff)
        };

        var teachers = new List<Teacher>();

        for (int i = 0; i < teacherData.Length; i++)
        {
            var data = teacherData[i];
            var teacher = new Teacher
            {
                Id = TeacherIds[i],
                FirstName = data.FirstName,
                LastName = data.LastName,
                Prefix = string.IsNullOrEmpty(data.Prefix) ? null : data.Prefix,
                Email = $"{data.FirstName.ToLower()}.{data.LastName.ToLower().Replace(" ", "")}@muziekschool.nl",
                Phone = $"+31 6 {(12345678 + i):D8}",
                Address = $"Muziekstraat {100 + i}",
                PostalCode = $"80{10 + i} AB",
                City = "Zwolle",
                HourlyRate = data.HourlyRate,
                IsActive = true,
                Role = data.Role,
                Notes = i == 0 ? "Senior teacher - specializes in classical piano" : null,
                CreatedAt = DateTime.UtcNow.AddMonths(-12),
                UpdatedAt = DateTime.UtcNow.AddMonths(-12)
            };

            teachers.Add(teacher);

            // Create TeacherInstrument relationships
            foreach (var instrumentId in data.InstrumentIds)
            {
                _context.TeacherInstruments.Add(new TeacherInstrument
                {
                    TeacherId = teacher.Id,
                    InstrumentId = instrumentId
                });
            }
        }

        await _context.Teachers.AddRangeAsync(teachers, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return teachers;
    }

    private async Task<List<CourseType>> SeedCourseTypesAsync(List<Instrument> instruments, CancellationToken cancellationToken)
    {
        // Check if course types already exist
        if (await _context.CourseTypes.AnyAsync(cancellationToken))
        {
            return await _context.CourseTypes.ToListAsync(cancellationToken);
        }

        var courseTypes = new List<CourseType>();
        var guidIndex = 0;

        foreach (var instrument in instruments)
        {
            // Individual lessons (30 min and 45 min)
            courseTypes.Add(new CourseType
            {
                Id = Guid.Parse($"20000001-0001-0001-0001-{guidIndex++:D12}"),
                InstrumentId = instrument.Id,
                Name = $"{instrument.Name} - Individual 30min",
                DurationMinutes = 30,
                Type = CourseTypeCategory.Individual,
                MaxStudents = 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddMonths(-12),
                UpdatedAt = DateTime.UtcNow.AddMonths(-12)
            });

            courseTypes.Add(new CourseType
            {
                Id = Guid.Parse($"20000001-0001-0001-0001-{guidIndex++:D12}"),
                InstrumentId = instrument.Id,
                Name = $"{instrument.Name} - Individual 45min",
                DurationMinutes = 45,
                Type = CourseTypeCategory.Individual,
                MaxStudents = 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddMonths(-12),
                UpdatedAt = DateTime.UtcNow.AddMonths(-12)
            });

            // Group lessons (only for some instruments)
            if (new[] { 1, 2, 4, 6 }.Contains(instrument.Id)) // Piano, Guitar, Drums, Vocals
            {
                courseTypes.Add(new CourseType
                {
                    Id = Guid.Parse($"20000001-0001-0001-0001-{guidIndex++:D12}"),
                    InstrumentId = instrument.Id,
                    Name = $"{instrument.Name} - Group",
                    DurationMinutes = 60,
                    Type = CourseTypeCategory.Group,
                    MaxStudents = 6,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddMonths(-12),
                    UpdatedAt = DateTime.UtcNow.AddMonths(-12)
                });
            }

            // Workshop (only for popular instruments)
            if (new[] { 2, 4, 6 }.Contains(instrument.Id)) // Guitar, Drums, Vocals
            {
                courseTypes.Add(new CourseType
                {
                    Id = Guid.Parse($"20000001-0001-0001-0001-{guidIndex++:D12}"),
                    InstrumentId = instrument.Id,
                    Name = $"{instrument.Name} - Workshop",
                    DurationMinutes = 120,
                    Type = CourseTypeCategory.Workshop,
                    MaxStudents = 12,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddMonths(-12),
                    UpdatedAt = DateTime.UtcNow.AddMonths(-12)
                });
            }
        }

        await _context.CourseTypes.AddRangeAsync(courseTypes, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return courseTypes;
    }

    private async Task<List<CourseTypePricingVersion>> SeedPricingVersionsAsync(List<CourseType> courseTypes, CancellationToken cancellationToken)
    {
        if (await _context.CourseTypePricingVersions.AnyAsync(cancellationToken))
        {
            return await _context.CourseTypePricingVersions.ToListAsync(cancellationToken);
        }

        var pricingVersions = new List<CourseTypePricingVersion>();
        var guidIndex = 0;

        foreach (var courseType in courseTypes)
        {
            // Base price calculation based on duration and type
            decimal basePriceAdult = courseType.DurationMinutes switch
            {
                30 => 30m,
                45 => 42m,
                60 => 25m, // per person for group
                120 => 35m, // per person for workshop
                _ => 30m
            };

            // Adjust for category
            if (courseType.Type == CourseTypeCategory.Group)
            {
                basePriceAdult *= 0.8m; // 20% discount for group
            }
            else if (courseType.Type == CourseTypeCategory.Workshop)
            {
                basePriceAdult *= 0.6m; // 40% discount for workshops
            }

            // Historical pricing (previous year - no longer current)
            pricingVersions.Add(new CourseTypePricingVersion
            {
                Id = Guid.Parse($"30000001-0001-0001-0001-{guidIndex++:D12}"),
                CourseTypeId = courseType.Id,
                PriceAdult = basePriceAdult - 2m, // Slightly lower last year
                PriceChild = (basePriceAdult - 2m) * 0.9m, // 10% child discount
                ValidFrom = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-2)),
                ValidUntil = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1).AddDays(-1)),
                IsCurrent = false,
                CreatedAt = DateTime.UtcNow.AddYears(-2),
                UpdatedAt = DateTime.UtcNow.AddYears(-1)
            });

            // Current pricing
            pricingVersions.Add(new CourseTypePricingVersion
            {
                Id = Guid.Parse($"30000001-0001-0001-0001-{guidIndex++:D12}"),
                CourseTypeId = courseType.Id,
                PriceAdult = basePriceAdult,
                PriceChild = basePriceAdult * 0.9m, // 10% child discount
                ValidFrom = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)),
                ValidUntil = null,
                IsCurrent = true,
                CreatedAt = DateTime.UtcNow.AddYears(-1),
                UpdatedAt = DateTime.UtcNow.AddYears(-1)
            });
        }

        await _context.CourseTypePricingVersions.AddRangeAsync(pricingVersions, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return pricingVersions;
    }

    private async Task SeedTeacherCourseTypesAsync(List<Teacher> teachers, List<CourseType> courseTypes, CancellationToken cancellationToken)
    {
        if (await _context.TeacherCourseTypes.AnyAsync(cancellationToken))
        {
            return;
        }

        // Get teacher instruments to map
        var teacherInstruments = await _context.TeacherInstruments.ToListAsync(cancellationToken);
        var teacherCourseTypes = new List<TeacherCourseType>();

        foreach (var teacher in teachers)
        {
            var instrumentIds = teacherInstruments
                .Where(ti => ti.TeacherId == teacher.Id)
                .Select(ti => ti.InstrumentId)
                .ToList();

            foreach (var courseType in courseTypes.Where(ct => instrumentIds.Contains(ct.InstrumentId)))
            {
                teacherCourseTypes.Add(new TeacherCourseType
                {
                    TeacherId = teacher.Id,
                    CourseTypeId = courseType.Id
                });
            }
        }

        await _context.TeacherCourseTypes.AddRangeAsync(teacherCourseTypes, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<List<Student>> SeedStudentsAsync(CancellationToken cancellationToken)
    {
        if (await _context.Students.AnyAsync(cancellationToken))
        {
            return await _context.Students.ToListAsync(cancellationToken);
        }

        var students = new List<Student>();
        var random = new Random(42); // Fixed seed for reproducibility

        // Student data with various ages and statuses
        var studentData = new[]
        {
            // Active adults
            (FirstName: "Emma", LastName: "de Groot", Age: 28, Status: StudentStatus.Active, HasBillingContact: false),
            (FirstName: "Liam", LastName: "Meijer", Age: 35, Status: StudentStatus.Active, HasBillingContact: false),
            (FirstName: "Olivia", LastName: "van Dijk", Age: 42, Status: StudentStatus.Active, HasBillingContact: false),
            (FirstName: "Noah", LastName: "Bos", Age: 31, Status: StudentStatus.Active, HasBillingContact: false),
            (FirstName: "Sophie", LastName: "van der Linden", Age: 26, Status: StudentStatus.Active, HasBillingContact: false),
            // Active children (with billing contacts - parents)
            (FirstName: "Max", LastName: "Koning", Age: 10, Status: StudentStatus.Active, HasBillingContact: true),
            (FirstName: "Eva", LastName: "Willems", Age: 12, Status: StudentStatus.Active, HasBillingContact: true),
            (FirstName: "Finn", LastName: "van Leeuwen", Age: 8, Status: StudentStatus.Active, HasBillingContact: true),
            (FirstName: "Julia", LastName: "Hendriks", Age: 14, Status: StudentStatus.Active, HasBillingContact: true),
            (FirstName: "Sem", LastName: "Dekker", Age: 16, Status: StudentStatus.Active, HasBillingContact: true),
            (FirstName: "Sara", LastName: "Peters", Age: 11, Status: StudentStatus.Active, HasBillingContact: true),
            (FirstName: "Luuk", LastName: "Vermeer", Age: 9, Status: StudentStatus.Active, HasBillingContact: true),
            // Active teens
            (FirstName: "Anna", LastName: "van der Berg", Age: 17, Status: StudentStatus.Active, HasBillingContact: true),
            (FirstName: "Tim", LastName: "Kuiper", Age: 15, Status: StudentStatus.Active, HasBillingContact: true),
            // Trial students
            (FirstName: "Lisa", LastName: "Mulder", Age: 25, Status: StudentStatus.Trial, HasBillingContact: false),
            (FirstName: "Tom", LastName: "de Boer", Age: 33, Status: StudentStatus.Trial, HasBillingContact: false),
            (FirstName: "Mila", LastName: "Janssen", Age: 7, Status: StudentStatus.Trial, HasBillingContact: true),
            // Inactive students (former)
            (FirstName: "Lars", LastName: "Scholten", Age: 29, Status: StudentStatus.Inactive, HasBillingContact: false),
            (FirstName: "Noor", LastName: "de Wit", Age: 38, Status: StudentStatus.Inactive, HasBillingContact: false),
            (FirstName: "Daan", LastName: "Kok", Age: 13, Status: StudentStatus.Inactive, HasBillingContact: true),
            // More active students for group/workshop variety
            (FirstName: "Tess", LastName: "Brouwer", Age: 22, Status: StudentStatus.Active, HasBillingContact: false),
            (FirstName: "Jesse", LastName: "Klein", Age: 19, Status: StudentStatus.Active, HasBillingContact: false),
            (FirstName: "Fleur", LastName: "Vos", Age: 45, Status: StudentStatus.Active, HasBillingContact: false),
            (FirstName: "Ruben", LastName: "de Ruiter", Age: 52, Status: StudentStatus.Active, HasBillingContact: false),
            (FirstName: "Isa", LastName: "van Dam", Age: 30, Status: StudentStatus.Active, HasBillingContact: false)
        };

        for (int i = 0; i < studentData.Length; i++)
        {
            var data = studentData[i];
            var dob = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-data.Age).AddDays(random.Next(-180, 180)));
            var enrolledMonthsAgo = data.Status == StudentStatus.Trial ? random.Next(0, 1) : random.Next(1, 24);

            var student = new Student
            {
                Id = Guid.Parse($"40000001-0001-0001-0001-{i:D12}"),
                FirstName = data.FirstName,
                LastName = data.LastName,
                Prefix = null,
                Email = $"{data.FirstName.ToLower()}.{data.LastName.ToLower().Replace(" ", "")}@example.com",
                Phone = $"+31 6 {(20000000 + i):D8}",
                PhoneAlt = i % 3 == 0 ? $"+31 6 {(30000000 + i):D8}" : null,
                Address = $"Leerlingstraat {200 + i}",
                PostalCode = $"80{20 + (i % 10)} CD",
                City = "Zwolle",
                DateOfBirth = dob,
                Gender = (Gender)(i % 4),
                Status = data.Status,
                EnrolledAt = DateTime.UtcNow.AddMonths(-enrolledMonthsAgo),
                AutoDebit = random.Next(0, 2) == 1,
                Notes = data.Status == StudentStatus.Inactive ? "Former student - moved away" : null,
                RegistrationFeePaidAt = data.Status != StudentStatus.Inactive
                    ? DateTime.UtcNow.AddMonths(-enrolledMonthsAgo)
                    : (DateTime?)null,
                CreatedAt = DateTime.UtcNow.AddMonths(-enrolledMonthsAgo),
                UpdatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 30))
            };

            // Add billing contact for children
            if (data.HasBillingContact)
            {
                student.BillingContactName = $"Parent of {data.FirstName}";
                student.BillingContactEmail = $"parent.{data.LastName.ToLower().Replace(" ", "")}@example.com";
                student.BillingContactPhone = $"+31 6 {(40000000 + i):D8}";
            }

            students.Add(student);
        }

        await _context.Students.AddRangeAsync(students, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return students;
    }

    private async Task<List<Course>> SeedCoursesAsync(List<Teacher> teachers, List<CourseType> courseTypes, List<Room> rooms, CancellationToken cancellationToken)
    {
        if (await _context.Courses.AnyAsync(cancellationToken))
        {
            return await _context.Courses.ToListAsync(cancellationToken);
        }

        var courses = new List<Course>();
        var teacherCourseTypes = await _context.TeacherCourseTypes.ToListAsync(cancellationToken);
        var guidIndex = 0;
        var random = new Random(42);

        // Create courses for each course type with different recurrences and statuses
        foreach (var courseType in courseTypes)
        {
            // Find teachers who can teach this course type
            var eligibleTeacherIds = teacherCourseTypes
                .Where(tct => tct.CourseTypeId == courseType.Id)
                .Select(tct => tct.TeacherId)
                .ToList();

            if (!eligibleTeacherIds.Any()) continue;

            // Determine room based on course type
            var room = courseType.Type switch
            {
                CourseTypeCategory.Individual => rooms.FirstOrDefault(r => r.Capacity <= 2),
                CourseTypeCategory.Group => rooms.FirstOrDefault(r => r.Capacity >= 4 && r.Capacity < 10),
                CourseTypeCategory.Workshop => rooms.FirstOrDefault(r => r.Capacity >= 10),
                _ => rooms.FirstOrDefault()
            };

            // Create 1-3 courses per course type
            var coursesPerType = courseType.Type switch
            {
                CourseTypeCategory.Individual => 3, // More individual courses
                CourseTypeCategory.Group => 2,
                CourseTypeCategory.Workshop => 1,
                _ => 2
            };

            for (int c = 0; c < coursesPerType; c++)
            {
                var teacherId = eligibleTeacherIds[random.Next(eligibleTeacherIds.Count)];
                var dayOfWeek = (DayOfWeek)(random.Next(1, 6)); // Monday-Friday
                var startHour = random.Next(9, 20); // 9:00 - 20:00
                var startTime = new TimeOnly(startHour, random.Next(0, 2) * 30); // 0 or 30 minutes
                var endTime = startTime.AddMinutes(courseType.DurationMinutes);

                // Determine frequency - weighted towards weekly
                var frequencyRoll = random.Next(100);
                var frequency = frequencyRoll < 70 ? CourseFrequency.Weekly :
                               frequencyRoll < 90 ? CourseFrequency.Biweekly :
                               CourseFrequency.Monthly;

                // Week parity for biweekly
                var weekParity = frequency == CourseFrequency.Biweekly
                    ? (random.Next(2) == 0 ? WeekParity.Odd : WeekParity.Even)
                    : WeekParity.All;

                // Determine status - most active, some completed/cancelled
                var statusRoll = random.Next(100);
                var status = statusRoll < 75 ? CourseStatus.Active :
                            statusRoll < 85 ? CourseStatus.Completed :
                            statusRoll < 92 ? CourseStatus.Paused :
                            CourseStatus.Cancelled;

                // Determine dates
                var startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-random.Next(1, 18)));
                DateOnly? endDate = status switch
                {
                    CourseStatus.Completed => DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-random.Next(1, 3))),
                    CourseStatus.Cancelled => DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-random.Next(0, 2))),
                    _ => null
                };

                // Determine if trial
                var isTrial = random.Next(100) < 10; // 10% trial courses

                courses.Add(new Course
                {
                    Id = Guid.Parse($"50000001-0001-0001-0001-{guidIndex++:D12}"),
                    TeacherId = teacherId,
                    CourseTypeId = courseType.Id,
                    RoomId = room?.Id,
                    DayOfWeek = dayOfWeek,
                    StartTime = startTime,
                    EndTime = endTime,
                    Frequency = frequency,
                    WeekParity = weekParity,
                    StartDate = startDate,
                    EndDate = endDate,
                    Status = status,
                    IsWorkshop = courseType.Type == CourseTypeCategory.Workshop,
                    IsTrial = isTrial,
                    Notes = isTrial ? "Trial course for new students" : null,
                    CreatedAt = startDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                    UpdatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 30))
                });
            }
        }

        await _context.Courses.AddRangeAsync(courses, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return courses;
    }

    private async Task<List<Enrollment>> SeedEnrollmentsAsync(List<Student> students, List<Course> courses, CancellationToken cancellationToken)
    {
        if (await _context.Enrollments.AnyAsync(cancellationToken))
        {
            return await _context.Enrollments.ToListAsync(cancellationToken);
        }

        var enrollments = new List<Enrollment>();
        var courseTypes = await _context.CourseTypes.ToListAsync(cancellationToken);
        var random = new Random(42);
        var guidIndex = 0;

        // Active students get enrolled in courses
        var activeStudents = students.Where(s => s.Status != StudentStatus.Inactive).ToList();
        var activeCourses = courses.Where(c => c.Status == CourseStatus.Active || c.Status == CourseStatus.Completed).ToList();

        foreach (var student in activeStudents)
        {
            // Each student enrolls in 1-3 courses
            var coursesToEnroll = random.Next(1, 4);
            var enrolledCourseIds = new HashSet<Guid>();

            for (int i = 0; i < coursesToEnroll; i++)
            {
                // Find an appropriate course
                var availableCourses = activeCourses
                    .Where(c => !enrolledCourseIds.Contains(c.Id))
                    .ToList();

                if (!availableCourses.Any()) break;

                var course = availableCourses[random.Next(availableCourses.Count)];
                var courseType = courseTypes.First(ct => ct.Id == course.CourseTypeId);

                // Check max students for group/workshop
                if (courseType.Type != CourseTypeCategory.Individual)
                {
                    var existingEnrollments = enrollments.Count(e => e.CourseId == course.Id);
                    if (existingEnrollments >= courseType.MaxStudents) continue;
                }

                enrolledCourseIds.Add(course.Id);

                // Determine enrollment status
                var enrollmentStatus = student.Status == StudentStatus.Trial
                    ? EnrollmentStatus.Trail
                    : course.Status == CourseStatus.Completed
                        ? EnrollmentStatus.Completed
                        : EnrollmentStatus.Active;

                // Discount based on number of courses
                var discountType = DiscountType.None;
                var discountPercent = 0m;

                if (enrolledCourseIds.Count > 1)
                {
                    discountType = DiscountType.Course;
                    discountPercent = 10m;
                }
                else if (random.Next(100) < 15) // 15% chance of family discount
                {
                    discountType = DiscountType.Family;
                    discountPercent = 10m;
                }

                enrollments.Add(new Enrollment
                {
                    Id = Guid.Parse($"60000001-0001-0001-0001-{guidIndex++:D12}"),
                    StudentId = student.Id,
                    CourseId = course.Id,
                    EnrolledAt = course.StartDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddDays(random.Next(-7, 14)),
                    DiscountPercent = discountPercent,
                    DiscountType = discountType,
                    Status = enrollmentStatus,
                    Notes = discountType == DiscountType.Family ? "Family member discount applied" : null,
                    CreatedAt = course.StartDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                    UpdatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 30))
                });
            }
        }

        await _context.Enrollments.AddRangeAsync(enrollments, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return enrollments;
    }

    private async Task<List<Lesson>> SeedLessonsAsync(List<Course> courses, List<Enrollment> enrollments, CancellationToken cancellationToken)
    {
        if (await _context.Lessons.AnyAsync(cancellationToken))
        {
            return await _context.Lessons.ToListAsync(cancellationToken);
        }

        var lessons = new List<Lesson>();
        var guidIndex = 0;
        var random = new Random(42);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Preload course types to avoid async queries in loop
        var courseTypes = await _context.CourseTypes.ToDictionaryAsync(ct => ct.Id, cancellationToken);

        foreach (var course in courses)
        {
            // Get enrollments for this course
            var courseEnrollments = enrollments.Where(e => e.CourseId == course.Id).ToList();
            if (!courseEnrollments.Any()) continue;

            // Generate lessons from start date until end date or today
            var endDate = course.EndDate ?? today.AddMonths(2); // Future lessons for 2 months
            var currentDate = course.StartDate;

            while (currentDate <= endDate)
            {
                // Check if this date matches the course schedule
                if (currentDate.DayOfWeek == course.DayOfWeek)
                {
                    // Check week parity for biweekly
                    if (course.Frequency == CourseFrequency.Biweekly)
                    {
                        var isoWeek = ISOWeek.GetWeekOfYear(currentDate.ToDateTime(TimeOnly.MinValue));
                        var isOddWeek = isoWeek % 2 == 1;

                        if ((course.WeekParity == WeekParity.Odd && !isOddWeek) ||
                            (course.WeekParity == WeekParity.Even && isOddWeek))
                        {
                            currentDate = currentDate.AddDays(1);
                            continue;
                        }
                    }

                    // For individual lessons, create one per student
                    // For group/workshop, create one lesson with multiple students
                    courseTypes.TryGetValue(course.CourseTypeId, out var courseType);

                    if (courseType?.Type == CourseTypeCategory.Individual)
                    {
                        foreach (var enrollment in courseEnrollments)
                        {
                            var isPast = currentDate < today;
                            var status = isPast
                                ? (random.Next(100) < 90 ? LessonStatus.Completed :
                                   random.Next(100) < 50 ? LessonStatus.Cancelled : LessonStatus.NoShow)
                                : LessonStatus.Scheduled;

                            lessons.Add(new Lesson
                            {
                                Id = Guid.Parse($"70000001-0001-0001-0001-{guidIndex++:D12}"),
                                CourseId = course.Id,
                                StudentId = enrollment.StudentId,
                                TeacherId = course.TeacherId,
                                RoomId = course.RoomId,
                                ScheduledDate = currentDate,
                                StartTime = course.StartTime,
                                EndTime = course.EndTime,
                                Status = status,
                                CancellationReason = status == LessonStatus.Cancelled
                                    ? (random.Next(2) == 0 ? "Student sick" : "Teacher unavailable")
                                    : null,
                                IsInvoiced = isPast && status == LessonStatus.Completed,
                                IsPaidToTeacher = isPast && status == LessonStatus.Completed && random.Next(100) < 80,
                                Notes = null,
                                CreatedAt = course.StartDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                                UpdatedAt = isPast ? currentDate.ToDateTime(course.EndTime, DateTimeKind.Utc) : DateTime.UtcNow
                            });
                        }
                    }
                    else
                    {
                        // Group/Workshop - one lesson for all enrolled students
                        var isPast = currentDate < today;
                        var status = isPast
                            ? (random.Next(100) < 95 ? LessonStatus.Completed : LessonStatus.Cancelled)
                            : LessonStatus.Scheduled;

                        lessons.Add(new Lesson
                        {
                            Id = Guid.Parse($"70000001-0001-0001-0001-{guidIndex++:D12}"),
                            CourseId = course.Id,
                            StudentId = null, // Group lesson - no specific student
                            TeacherId = course.TeacherId,
                            RoomId = course.RoomId,
                            ScheduledDate = currentDate,
                            StartTime = course.StartTime,
                            EndTime = course.EndTime,
                            Status = status,
                            CancellationReason = status == LessonStatus.Cancelled ? "Low attendance" : null,
                            IsInvoiced = isPast && status == LessonStatus.Completed,
                            IsPaidToTeacher = isPast && status == LessonStatus.Completed && random.Next(100) < 80,
                            Notes = courseType?.Type == CourseTypeCategory.Workshop ? "Workshop session" : "Group lesson",
                            CreatedAt = course.StartDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                            UpdatedAt = isPast ? currentDate.ToDateTime(course.EndTime, DateTimeKind.Utc) : DateTime.UtcNow
                        });
                    }
                }

                // Move to next day (weekly check handled above)
                currentDate = course.Frequency == CourseFrequency.Monthly
                    ? currentDate.AddMonths(1)
                    : currentDate.AddDays(1);
            }
        }

        await _context.Lessons.AddRangeAsync(lessons, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return lessons;
    }

    private async Task<List<Invoice>> SeedInvoicesAsync(
        List<Student> students,
        List<Lesson> lessons,
        List<CourseTypePricingVersion> pricingVersions,
        CancellationToken cancellationToken)
    {
        if (await _context.Invoices.AnyAsync(cancellationToken))
        {
            return await _context.Invoices.ToListAsync(cancellationToken);
        }

        var invoices = new List<Invoice>();
        var invoiceLines = new List<InvoiceLine>();
        var random = new Random(42);
        var invoiceNumber = 1;
        var lineId = 1;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Get completed, invoiced lessons grouped by student and month
        var invoicedLessons = lessons
            .Where(l => l.IsInvoiced && l.Status == LessonStatus.Completed)
            .GroupBy(l => new { l.StudentId, Month = new DateOnly(l.ScheduledDate.Year, l.ScheduledDate.Month, 1) })
            .ToList();

        // Get enrollments and course types for pricing
        var enrollments = await _context.Enrollments.ToListAsync(cancellationToken);
        var courses = await _context.Courses.ToListAsync(cancellationToken);
        var courseTypes = await _context.CourseTypes.ToListAsync(cancellationToken);

        foreach (var group in invoicedLessons)
        {
            if (group.Key.StudentId == null) continue;

            var student = students.FirstOrDefault(s => s.Id == group.Key.StudentId);
            if (student == null) continue;

            var invoiceId = Guid.Parse($"80000001-0001-0001-0001-{invoiceNumber - 1:D12}");
            var issueDate = group.Key.Month.AddMonths(1).AddDays(random.Next(1, 10));
            var dueDate = issueDate.AddDays(14);

            decimal subtotal = 0;
            var studentAge = student.DateOfBirth.HasValue
                ? DateTime.UtcNow.Year - student.DateOfBirth.Value.Year
                : 30; // Default adult
            var isChild = studentAge < ChildAgeLimit;

            // Create invoice lines for each lesson
            foreach (var lesson in group)
            {
                var course = courses.FirstOrDefault(c => c.Id == lesson.CourseId);
                var courseType = course != null ? courseTypes.FirstOrDefault(ct => ct.Id == course.CourseTypeId) : null;
                var enrollment = enrollments.FirstOrDefault(e => e.StudentId == student.Id && e.CourseId == lesson.CourseId);

                // Get current pricing version
                var pricing = pricingVersions
                    .Where(pv => pv.CourseTypeId == course?.CourseTypeId && pv.IsCurrent)
                    .FirstOrDefault();

                if (pricing == null) continue;

                var unitPrice = isChild ? pricing.PriceChild : pricing.PriceAdult;

                // Apply enrollment discount
                if (enrollment?.DiscountPercent > 0)
                {
                    unitPrice *= (1 - enrollment.DiscountPercent / 100);
                }

                var lineTotal = unitPrice;
                subtotal += lineTotal;

                invoiceLines.Add(new InvoiceLine
                {
                    Id = lineId++,
                    InvoiceId = invoiceId,
                    LessonId = lesson.Id,
                    PricingVersionId = pricing.Id,
                    Description = $"{courseType?.Name ?? "Lesson"} - {lesson.ScheduledDate:d MMM yyyy}",
                    Quantity = 1,
                    UnitPrice = unitPrice,
                    VatRate = VatRate,
                    LineTotal = lineTotal
                });
            }

            // Add registration fee line for first invoice
            var hasRegistrationFeeInvoice = invoices.Any(i => i.StudentId == student.Id);
            if (!hasRegistrationFeeInvoice && student.RegistrationFeePaidAt.HasValue)
            {
                invoiceLines.Add(new InvoiceLine
                {
                    Id = lineId++,
                    InvoiceId = invoiceId,
                    LessonId = null,
                    PricingVersionId = null,
                    Description = "Eenmalig inschrijfgeld",
                    Quantity = 1,
                    UnitPrice = RegistrationFee,
                    VatRate = VatRate,
                    LineTotal = RegistrationFee
                });
                subtotal += RegistrationFee;
            }

            var vatAmount = Math.Round(subtotal * (VatRate / 100), 2);
            var total = subtotal + vatAmount;

            // Determine invoice status
            var isPast = dueDate < today;
            var status = isPast
                ? (random.Next(100) < 80 ? InvoiceStatus.Paid :
                   random.Next(100) < 50 ? InvoiceStatus.Overdue : InvoiceStatus.Sent)
                : (random.Next(100) < 30 ? InvoiceStatus.Sent : InvoiceStatus.Draft);

            invoices.Add(new Invoice
            {
                Id = invoiceId,
                InvoiceNumber = $"NMI-{issueDate.Year}-{invoiceNumber:D5}",
                StudentId = student.Id,
                IssueDate = issueDate,
                DueDate = dueDate,
                Subtotal = subtotal,
                VatAmount = vatAmount,
                Total = total,
                DiscountAmount = 0,
                Status = status,
                PaidAt = status == InvoiceStatus.Paid
                    ? dueDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddDays(random.Next(-5, 10))
                    : null,
                PaymentMethod = status == InvoiceStatus.Paid
                    ? (random.Next(2) == 0 ? "Bank" : "DirectDebit")
                    : null,
                Notes = status == InvoiceStatus.Overdue ? "Payment reminder sent" : null,
                CreatedAt = issueDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                UpdatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 30))
            });

            invoiceNumber++;
        }

        await _context.Invoices.AddRangeAsync(invoices, cancellationToken);
        await _context.InvoiceLines.AddRangeAsync(invoiceLines, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return invoices;
    }

    private async Task SeedPaymentsAsync(List<Invoice> invoices, CancellationToken cancellationToken)
    {
        if (await _context.Payments.AnyAsync(cancellationToken))
        {
            return;
        }

        var payments = new List<Payment>();
        var random = new Random(42);
        var guidIndex = 0;

        foreach (var invoice in invoices.Where(i => i.Status == InvoiceStatus.Paid))
        {
            var method = invoice.PaymentMethod switch
            {
                "Bank" => PaymentMethod.Bank,
                "DirectDebit" => PaymentMethod.DirectDebit,
                "Cash" => PaymentMethod.Cash,
                "Card" => PaymentMethod.Card,
                _ => PaymentMethod.Bank
            };

            payments.Add(new Payment
            {
                Id = Guid.Parse($"90000001-0001-0001-0001-{guidIndex++:D12}"),
                InvoiceId = invoice.Id,
                Amount = invoice.Total,
                PaymentDate = DateOnly.FromDateTime(invoice.PaidAt!.Value),
                Method = method,
                Reference = method == PaymentMethod.Bank || method == PaymentMethod.DirectDebit
                    ? $"TXN{random.Next(100000, 999999)}"
                    : null,
                RecordedById = null,
                Notes = null,
                CreatedAt = invoice.PaidAt!.Value,
                UpdatedAt = invoice.PaidAt!.Value
            });
        }

        await _context.Payments.AddRangeAsync(payments, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedLedgerEntriesAsync(List<Student> students, List<Course> courses, Guid adminUserId, CancellationToken cancellationToken)
    {
        if (await _context.StudentLedgerEntries.AnyAsync(cancellationToken))
        {
            return;
        }

        var ledgerEntries = new List<StudentLedgerEntry>();
        var random = new Random(42);
        var guidIndex = 0;
        var refIndex = 1;

        // Create some open corrections for various students
        var activeStudents = students.Where(s => s.Status == StudentStatus.Active).Take(8).ToList();

        foreach (var student in activeStudents)
        {
            // Create 1-2 ledger entries per student
            var entryCount = random.Next(1, 3);

            for (int i = 0; i < entryCount; i++)
            {
                var entryType = random.Next(100) < 70 ? LedgerEntryType.Credit : LedgerEntryType.Debit;
                var status = random.Next(100) < 60 ? LedgerEntryStatus.Open :
                            random.Next(100) < 50 ? LedgerEntryStatus.PartiallyApplied :
                            LedgerEntryStatus.FullyApplied;

                var amount = Math.Round((decimal)(random.NextDouble() * 50 + 10), 2);

                var descriptions = entryType == LedgerEntryType.Credit
                    ? new[]
                    {
                        "Overpayment refund",
                        "Lesson cancellation credit",
                        "Promotional credit",
                        "Teacher absence compensation",
                        "Early payment discount"
                    }
                    : new[]
                    {
                        "Late payment fee",
                        "Material costs",
                        "Exam fee",
                        "Book rental",
                        "Additional practice room usage"
                    };

                ledgerEntries.Add(new StudentLedgerEntry
                {
                    Id = Guid.Parse($"A0000001-0001-0001-0001-{guidIndex++:D12}"),
                    CorrectionRefName = $"COR-{DateTime.UtcNow.Year}-{refIndex++:D5}",
                    Description = descriptions[random.Next(descriptions.Length)],
                    StudentId = student.Id,
                    CourseId = random.Next(100) < 30 ? courses.FirstOrDefault()?.Id : null,
                    Amount = amount,
                    EntryType = entryType,
                    Status = status,
                    CreatedById = adminUserId,
                    CreatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 90)),
                    UpdatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 30))
                });
            }
        }

        await _context.StudentLedgerEntries.AddRangeAsync(ledgerEntries, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedHolidaysAsync(CancellationToken cancellationToken)
    {
        if (await _context.Holidays.AnyAsync(cancellationToken))
        {
            return;
        }

        var currentYear = DateTime.UtcNow.Year;

        var holidays = new List<Holiday>
        {
            new()
            {
                Id = 1,
                Name = "Kerstvakantie",
                StartDate = new DateOnly(currentYear, 12, 23),
                EndDate = new DateOnly(currentYear + 1, 1, 5)
            },
            new()
            {
                Id = 2,
                Name = "Voorjaarsvakantie",
                StartDate = new DateOnly(currentYear, 2, 17),
                EndDate = new DateOnly(currentYear, 2, 25)
            },
            new()
            {
                Id = 3,
                Name = "Meivakantie",
                StartDate = new DateOnly(currentYear, 4, 26),
                EndDate = new DateOnly(currentYear, 5, 4)
            },
            new()
            {
                Id = 4,
                Name = "Zomervakantie",
                StartDate = new DateOnly(currentYear, 7, 6),
                EndDate = new DateOnly(currentYear, 8, 17)
            },
            new()
            {
                Id = 5,
                Name = "Herfstvakantie",
                StartDate = new DateOnly(currentYear, 10, 19),
                EndDate = new DateOnly(currentYear, 10, 27)
            },
            new()
            {
                Id = 6,
                Name = "Goede Vrijdag",
                StartDate = new DateOnly(currentYear, 4, 18),
                EndDate = new DateOnly(currentYear, 4, 18)
            },
            new()
            {
                Id = 7,
                Name = "Paasmaandag",
                StartDate = new DateOnly(currentYear, 4, 21),
                EndDate = new DateOnly(currentYear, 4, 21)
            },
            new()
            {
                Id = 8,
                Name = "Koningsdag",
                StartDate = new DateOnly(currentYear, 4, 27),
                EndDate = new DateOnly(currentYear, 4, 27)
            },
            new()
            {
                Id = 9,
                Name = "Bevrijdingsdag",
                StartDate = new DateOnly(currentYear, 5, 5),
                EndDate = new DateOnly(currentYear, 5, 5)
            },
            new()
            {
                Id = 10,
                Name = "Hemelvaartsdag",
                StartDate = new DateOnly(currentYear, 5, 29),
                EndDate = new DateOnly(currentYear, 5, 29)
            },
            new()
            {
                Id = 11,
                Name = "Pinkstermaandag",
                StartDate = new DateOnly(currentYear, 6, 9),
                EndDate = new DateOnly(currentYear, 6, 9)
            }
        };

        await _context.Holidays.AddRangeAsync(holidays, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    #endregion
}
