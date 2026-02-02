using Microsoft.EntityFrameworkCore;
using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Infrastructure.Data;

namespace BosDAT.Infrastructure.Seeding.DataGenerators;

/// <summary>
/// Generates teacher data including teacher-instrument relationships.
/// </summary>
public class TeacherDataGenerator
{
    private readonly ApplicationDbContext _context;
    private readonly SeederContext _seederContext;

    // Teacher definitions: FirstName, LastName, InstrumentIds, HourlyRate, Role
    private static readonly (string FirstName, string LastName, int[] InstrumentIds, decimal HourlyRate, TeacherRole Role)[] TeacherDefinitions =
    {
        ("Maria", "van den Berg", new[] { 1, 10 }, 45m, TeacherRole.Teacher),   // Piano, Keyboard
        ("Jan", "de Vries", new[] { 2, 3 }, 40m, TeacherRole.Teacher),          // Guitar, Bass
        ("Sophie", "Jansen", new[] { 5 }, 50m, TeacherRole.Teacher),            // Violin
        ("Peter", "Bakker", new[] { 4 }, 42m, TeacherRole.Teacher),             // Drums
        ("Emma", "Visser", new[] { 6 }, 48m, TeacherRole.Teacher),              // Vocals
        ("Thomas", "de Jong", new[] { 7, 8, 9 }, 55m, TeacherRole.Teacher),     // Sax, Flute, Trumpet
        ("Lisa", "Mulder", new[] { 1, 2 }, 38m, TeacherRole.Teacher),           // Piano, Guitar (junior)
        ("David", "Smit", new[] { 4, 2 }, 43m, TeacherRole.Staff)               // Drums, Guitar (staff)
    };

    public TeacherDataGenerator(ApplicationDbContext context, SeederContext seederContext)
    {
        _context = context;
        _seederContext = seederContext;
    }

    public async Task<List<Teacher>> GenerateAsync(CancellationToken cancellationToken)
    {
        if (await _context.Teachers.AnyAsync(cancellationToken))
        {
            return await _context.Teachers.ToListAsync(cancellationToken);
        }

        var teachers = new List<Teacher>();
        var teacherInstruments = new List<TeacherInstrument>();
        var createdAt = DateTime.UtcNow.AddMonths(-12);

        for (int i = 0; i < TeacherDefinitions.Length; i++)
        {
            var def = TeacherDefinitions[i];
            var teacher = new Teacher
            {
                Id = SeederConstants.TeacherIds[i],
                FirstName = def.FirstName,
                LastName = def.LastName,
                Prefix = null,
                Email = $"{def.FirstName.ToLower()}.{def.LastName.ToLower().Replace(" ", "")}@muziekschool.nl",
                Phone = $"+31 6 {12345678 + i:D8}",
                Address = $"Muziekstraat {100 + i}",
                PostalCode = $"80{10 + i} AB",
                City = "Zwolle",
                HourlyRate = def.HourlyRate,
                IsActive = true,
                Role = def.Role,
                Notes = i == 0 ? "Senior teacher - specializes in classical piano" : null,
                CreatedAt = createdAt,
                UpdatedAt = createdAt
            };

            teachers.Add(teacher);

            // Create TeacherInstrument relationships
            foreach (var instrumentId in def.InstrumentIds)
            {
                teacherInstruments.Add(new TeacherInstrument
                {
                    TeacherId = teacher.Id,
                    InstrumentId = instrumentId
                });
            }
        }

        await _context.Teachers.AddRangeAsync(teachers, cancellationToken);
        await _context.TeacherInstruments.AddRangeAsync(teacherInstruments, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _seederContext.Teachers = teachers;
        return teachers;
    }

    public async Task GenerateCourseTypeLinksAsync(
        List<Teacher> teachers,
        List<CourseType> courseTypes,
        CancellationToken cancellationToken)
    {
        if (await _context.TeacherCourseTypes.AnyAsync(cancellationToken))
        {
            return;
        }

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
}
