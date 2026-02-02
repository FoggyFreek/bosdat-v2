using Microsoft.EntityFrameworkCore;
using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Infrastructure.Data;

namespace BosDAT.Infrastructure.Seeding.DataGenerators;

/// <summary>
/// Generates student data with various ages, statuses, and billing configurations.
/// </summary>
public class StudentDataGenerator
{
    private readonly ApplicationDbContext _context;
    private readonly SeederContext _seederContext;

    // Student definitions: FirstName, LastName, Age, Status, HasBillingContact
    private static readonly (string FirstName, string LastName, int Age, StudentStatus Status, bool HasBillingContact)[] StudentDefinitions =
    {
        // Active adults
        ("Emma", "de Groot", 28, StudentStatus.Active, false),
        ("Liam", "Meijer", 35, StudentStatus.Active, false),
        ("Olivia", "van Dijk", 42, StudentStatus.Active, false),
        ("Noah", "Bos", 31, StudentStatus.Active, false),
        ("Sophie", "van der Linden", 26, StudentStatus.Active, false),
        // Active children (with billing contacts - parents)
        ("Max", "Koning", 10, StudentStatus.Active, true),
        ("Eva", "Willems", 12, StudentStatus.Active, true),
        ("Finn", "van Leeuwen", 8, StudentStatus.Active, true),
        ("Julia", "Hendriks", 14, StudentStatus.Active, true),
        ("Sem", "Dekker", 16, StudentStatus.Active, true),
        ("Sara", "Peters", 11, StudentStatus.Active, true),
        ("Luuk", "Vermeer", 9, StudentStatus.Active, true),
        // Active teens
        ("Anna", "van der Berg", 17, StudentStatus.Active, true),
        ("Tim", "Kuiper", 15, StudentStatus.Active, true),
        // Trial students
        ("Lisa", "Mulder", 25, StudentStatus.Trial, false),
        ("Tom", "de Boer", 33, StudentStatus.Trial, false),
        ("Mila", "Janssen", 7, StudentStatus.Trial, true),
        // Inactive students (former)
        ("Lars", "Scholten", 29, StudentStatus.Inactive, false),
        ("Noor", "de Wit", 38, StudentStatus.Inactive, false),
        ("Daan", "Kok", 13, StudentStatus.Inactive, true),
        // More active students for group/workshop variety
        ("Tess", "Brouwer", 22, StudentStatus.Active, false),
        ("Jesse", "Klein", 19, StudentStatus.Active, false),
        ("Fleur", "Vos", 45, StudentStatus.Active, false),
        ("Ruben", "de Ruiter", 52, StudentStatus.Active, false),
        ("Isa", "van Dam", 30, StudentStatus.Active, false)
    };

    public StudentDataGenerator(ApplicationDbContext context, SeederContext seederContext)
    {
        _context = context;
        _seederContext = seederContext;
    }

    public async Task<List<Student>> GenerateAsync(CancellationToken cancellationToken)
    {
        var existingCount = await _context.Students.CountAsync(cancellationToken);
        if (existingCount > 0)
        {
            return await _context.Students.ToListAsync(cancellationToken);
        }

        var students = new List<Student>();

        for (int i = 0; i < StudentDefinitions.Length; i++)
        {
            var def = StudentDefinitions[i];
            var dob = DateOnly.FromDateTime(
                DateTime.UtcNow.AddYears(-def.Age).AddDays(_seederContext.NextInt(-180, 180)));
            var enrolledMonthsAgo = def.Status == StudentStatus.Trial
                ? _seederContext.NextInt(0, 1)
                : _seederContext.NextInt(1, 24);

            var student = new Student
            {
                Id = SeederConstants.GenerateStudentId(i),
                FirstName = def.FirstName,
                LastName = def.LastName,
                Prefix = null,
                Email = $"{def.FirstName.ToLower()}.{def.LastName.ToLower().Replace(" ", "")}@example.com",
                Phone = $"+31 6 {20000000 + i:D8}",
                PhoneAlt = i % 3 == 0 ? $"+31 6 {30000000 + i:D8}" : null,
                Address = $"Leerlingstraat {200 + i}",
                PostalCode = $"80{20 + (i % 10)} CD",
                City = "Zwolle",
                DateOfBirth = dob,
                Gender = (Gender)(i % 4),
                Status = def.Status,
                EnrolledAt = DateTime.UtcNow.AddMonths(-enrolledMonthsAgo),
                AutoDebit = _seederContext.NextBool(),
                Notes = def.Status == StudentStatus.Inactive ? "Former student - moved away" : null,
                RegistrationFeePaidAt = def.Status != StudentStatus.Inactive
                    ? DateTime.UtcNow.AddMonths(-enrolledMonthsAgo)
                    : null,
                CreatedAt = DateTime.UtcNow.AddMonths(-enrolledMonthsAgo),
                UpdatedAt = DateTime.UtcNow.AddDays(-_seederContext.NextInt(1, 30))
            };

            // Add billing contact for children
            if (def.HasBillingContact)
            {
                student.BillingContactName = $"Parent of {def.FirstName}";
                student.BillingContactEmail = $"parent.{def.LastName.ToLower().Replace(" ", "")}@example.com";
                student.BillingContactPhone = $"+31 6 {40000000 + i:D8}";
            }

            students.Add(student);
        }

        await _context.Students.AddRangeAsync(students, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _seederContext.Students = students;
        return students;
    }
}
