using Microsoft.EntityFrameworkCore;
using Xunit;
using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Infrastructure.Data;
using BosDAT.Infrastructure.Seeding;
using BosDAT.Infrastructure.Seeding.DataGenerators;

namespace BosDAT.Infrastructure.Tests.Services;

/// <summary>
/// Unit tests for individual data generators.
/// Tests each generator in isolation with focused assertions.
/// </summary>
public class DataGeneratorTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly SeederContext _seederContext;

    public DataGeneratorTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"GeneratorTest_{Guid.NewGuid()}")
            .Options;

        _context = new ApplicationDbContext(options);
        _seederContext = new SeederContext();

        // Seed reference data
        SeedReferenceData();
    }

    private void SeedReferenceData()
    {
        _context.Instruments.AddRange(
            new Instrument { Id = 1, Name = "Piano", Category = InstrumentCategory.Keyboard },
            new Instrument { Id = 2, Name = "Guitar", Category = InstrumentCategory.String },
            new Instrument { Id = 3, Name = "Bass Guitar", Category = InstrumentCategory.String },
            new Instrument { Id = 4, Name = "Drums", Category = InstrumentCategory.Percussion },
            new Instrument { Id = 5, Name = "Violin", Category = InstrumentCategory.String },
            new Instrument { Id = 6, Name = "Vocals", Category = InstrumentCategory.Vocal },
            new Instrument { Id = 7, Name = "Saxophone", Category = InstrumentCategory.Wind },
            new Instrument { Id = 8, Name = "Flute", Category = InstrumentCategory.Wind },
            new Instrument { Id = 9, Name = "Trumpet", Category = InstrumentCategory.Brass },
            new Instrument { Id = 10, Name = "Keyboard", Category = InstrumentCategory.Keyboard }
        );

        _context.Rooms.AddRange(
            new Room { Id = 1, Name = "Room 1", Capacity = 2, HasPiano = true },
            new Room { Id = 2, Name = "Room 2", Capacity = 2, HasPiano = true },
            new Room { Id = 3, Name = "Room 3", Capacity = 4, HasDrums = true },
            new Room { Id = 4, Name = "Room 4", Capacity = 6, HasAmplifier = true },
            new Room { Id = 5, Name = "Group Room", Capacity = 10, HasPiano = true }
        );

        _context.SaveChanges();

        _seederContext.Instruments = _context.Instruments.ToList();
        _seederContext.Rooms = _context.Rooms.ToList();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    #region SeederConstants Tests

    [Fact]
    public void SeederConstants_TeacherIds_HasEightEntries()
    {
        Assert.Equal(8, SeederConstants.TeacherIds.Length);
    }

    [Fact]
    public void SeederConstants_TeacherIds_AreUnique()
    {
        var distinct = SeederConstants.TeacherIds.Distinct().Count();
        Assert.Equal(SeederConstants.TeacherIds.Length, distinct);
    }

    [Fact]
    public void SeederConstants_GenerateCourseTypeId_ReturnsConsistentIds()
    {
        var id1 = SeederConstants.GenerateCourseTypeId(0);
        var id2 = SeederConstants.GenerateCourseTypeId(0);
        Assert.Equal(id1, id2);
    }

    [Fact]
    public void SeederConstants_GenerateCourseTypeId_ReturnsUniqueIds()
    {
        var id1 = SeederConstants.GenerateCourseTypeId(0);
        var id2 = SeederConstants.GenerateCourseTypeId(1);
        Assert.NotEqual(id1, id2);
    }

    #endregion

    #region SeederContext Tests

    [Fact]
    public void SeederContext_Random_IsConsistent()
    {
        var context1 = new SeederContext();
        var context2 = new SeederContext();

        // Same seed should produce same sequence
        Assert.Equal(context1.NextInt(0, 1000), context2.NextInt(0, 1000));
        Assert.Equal(context1.NextInt(0, 1000), context2.NextInt(0, 1000));
    }

    [Fact]
    public void SeederContext_NextCourseTypeId_AutoIncrements()
    {
        var id1 = _seederContext.NextCourseTypeId();
        var id2 = _seederContext.NextCourseTypeId();
        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void SeederContext_Reset_ClearsCounters()
    {
        _seederContext.NextCourseTypeId();
        _seederContext.NextCourseTypeId();
        _seederContext.Reset();

        var id = _seederContext.NextCourseTypeId();
        Assert.Equal(SeederConstants.GenerateCourseTypeId(0), id);
    }

    [Fact]
    public void SeederContext_GetRandomItem_ReturnsItemFromList()
    {
        var items = new[] { "a", "b", "c" };
        var item = _seederContext.GetRandomItem(items);
        Assert.Contains(item, items);
    }

    #endregion

    #region TeacherDataGenerator Tests

    [Fact]
    public async Task TeacherDataGenerator_GenerateAsync_Creates8Teachers()
    {
        // Arrange
        var generator = new TeacherDataGenerator(_context, _seederContext);

        // Act
        var teachers = await generator.GenerateAsync(CancellationToken.None);

        // Assert
        Assert.Equal(8, teachers.Count);
    }

    [Fact]
    public async Task TeacherDataGenerator_GenerateAsync_AllTeachersHaveValidEmails()
    {
        // Arrange
        var generator = new TeacherDataGenerator(_context, _seederContext);

        // Act
        var teachers = await generator.GenerateAsync(CancellationToken.None);

        // Assert
        Assert.All(teachers, t =>
        {
            Assert.NotNull(t.Email);
            Assert.Contains("@", t.Email);
            Assert.EndsWith("@muziekschool.nl", t.Email);
        });
    }

    [Fact]
    public async Task TeacherDataGenerator_GenerateAsync_CreatesTeacherInstrumentLinks()
    {
        // Arrange
        var generator = new TeacherDataGenerator(_context, _seederContext);

        // Act
        await generator.GenerateAsync(CancellationToken.None);
        var links = await _context.TeacherInstruments.ToListAsync();

        // Assert - At least one per teacher, some have multiple
        Assert.InRange(links.Count, 9, int.MaxValue);
    }

    [Fact]
    public async Task TeacherDataGenerator_GenerateAsync_IsIdempotent()
    {
        // Arrange
        var generator = new TeacherDataGenerator(_context, _seederContext);
        await generator.GenerateAsync(CancellationToken.None);

        // Act
        _ = await generator.GenerateAsync(CancellationToken.None);

        // Assert
        Assert.Equal(8, await _context.Teachers.CountAsync());
    }

    #endregion

    #region StudentDataGenerator Tests

    [Fact]
    public async Task StudentDataGenerator_GenerateAsync_Creates25Students()
    {
        // Arrange
        var generator = new StudentDataGenerator(_context, _seederContext);

        // Act
        var students = await generator.GenerateAsync(CancellationToken.None);

        // Assert
        Assert.Equal(25, students.Count);
    }

    [Fact]
    public async Task StudentDataGenerator_GenerateAsync_HasVariousStatuses()
    {
        // Arrange
        var generator = new StudentDataGenerator(_context, _seederContext);

        // Act
        var students = await generator.GenerateAsync(CancellationToken.None);

        // Assert
        Assert.Contains(students, s => s.Status == StudentStatus.Active);
        Assert.Contains(students, s => s.Status == StudentStatus.Trial);
        Assert.Contains(students, s => s.Status == StudentStatus.Inactive);
    }

    [Fact]
    public async Task StudentDataGenerator_GenerateAsync_ChildrenHaveBillingContacts()
    {
        // Arrange
        var generator = new StudentDataGenerator(_context, _seederContext);

        // Act
        var students = await generator.GenerateAsync(CancellationToken.None);
        var children = students.Where(s =>
            s.DateOfBirth.HasValue &&
            DateTime.UtcNow.Year - s.DateOfBirth.Value.Year < 18).ToList();

        // Assert
        var withBillingContact = children.Count(s => !string.IsNullOrEmpty(s.BillingContactEmail));
        Assert.NotEqual(0, withBillingContact);
    }

    #endregion

    #region CourseDataGenerator Tests

    [Fact]
    public async Task CourseDataGenerator_GenerateCourseTypesAsync_CreatesTypesForAllInstruments()
    {
        // Arrange
        var generator = new CourseDataGenerator(_context, _seederContext);
        var instruments = await _context.Instruments.ToListAsync();

        // Act
        var courseTypes = await generator.GenerateCourseTypesAsync(instruments, CancellationToken.None);
        var instrumentIds = courseTypes.Select(ct => ct.InstrumentId).Distinct().ToList();

        // Assert
        Assert.Equal(10, instrumentIds.Count);
    }

    [Fact]
    public async Task CourseDataGenerator_GenerateCourseTypesAsync_Creates30MinAnd45MinIndividual()
    {
        // Arrange
        var generator = new CourseDataGenerator(_context, _seederContext);
        var instruments = await _context.Instruments.ToListAsync();

        // Act
        var courseTypes = await generator.GenerateCourseTypesAsync(instruments, CancellationToken.None);
        var individual30 = courseTypes.Count(ct =>
            ct.Type == CourseTypeCategory.Individual && ct.DurationMinutes == 30);
        var individual45 = courseTypes.Count(ct =>
            ct.Type == CourseTypeCategory.Individual && ct.DurationMinutes == 45);

        // Assert
        Assert.Equal(10, individual30); // One per instrument
        Assert.Equal(10, individual45); // One per instrument
    }

    [Fact]
    public async Task CourseDataGenerator_GenerateCourseTypesAsync_GroupOnlyForSomeInstruments()
    {
        // Arrange
        var generator = new CourseDataGenerator(_context, _seederContext);
        var instruments = await _context.Instruments.ToListAsync();

        // Act
        var courseTypes = await generator.GenerateCourseTypesAsync(instruments, CancellationToken.None);
        var groupTypes = courseTypes.Where(ct => ct.Type == CourseTypeCategory.Group).ToList();

        // Assert - only Piano(1), Guitar(2), Drums(4), Vocals(6) have group
        Assert.Equal(4, groupTypes.Count);
        Assert.All(groupTypes, gt =>
            Assert.Contains(gt.InstrumentId, SeederConstants.GroupLessonInstrumentIds));
    }

    [Fact]
    public async Task CourseDataGenerator_GeneratePricingVersionsAsync_CreatesTwoPerCourseType()
    {
        // Arrange
        var generator = new CourseDataGenerator(_context, _seederContext);
        var instruments = await _context.Instruments.ToListAsync();
        var courseTypes = await generator.GenerateCourseTypesAsync(instruments, CancellationToken.None);

        // Act
        var pricingVersions = await generator.GeneratePricingVersionsAsync(courseTypes, CancellationToken.None);

        // Assert - 2 versions per course type (historical + current)
        Assert.Equal(courseTypes.Count * 2, pricingVersions.Count);
    }

    [Fact]
    public async Task CourseDataGenerator_GeneratePricingVersionsAsync_OneCurrentPerCourseType()
    {
        // Arrange
        var generator = new CourseDataGenerator(_context, _seederContext);
        var instruments = await _context.Instruments.ToListAsync();
        var courseTypes = await generator.GenerateCourseTypesAsync(instruments, CancellationToken.None);

        // Act
        var pricingVersions = await generator.GeneratePricingVersionsAsync(courseTypes, CancellationToken.None);

        // Assert
        foreach (var ct in courseTypes)
        {
            var currentVersions = pricingVersions.Count(pv => pv.CourseTypeId == ct.Id && pv.IsCurrent);
            Assert.Equal(1, currentVersions);
        }
    }

    [Fact]
    public async Task CourseDataGenerator_GeneratePricingVersionsAsync_ChildPriceLessThanAdult()
    {
        // Arrange
        var generator = new CourseDataGenerator(_context, _seederContext);
        var instruments = await _context.Instruments.ToListAsync();
        var courseTypes = await generator.GenerateCourseTypesAsync(instruments, CancellationToken.None);

        // Act
        var pricingVersions = await generator.GeneratePricingVersionsAsync(courseTypes, CancellationToken.None);

        // Assert
        Assert.All(pricingVersions, pv =>
            Assert.True(pv.PriceChild < pv.PriceAdult));
    }

    #endregion

    #region LessonDataGenerator Tests

    [Fact]
    public async Task LessonDataGenerator_GenerateAsync_CreatesLessonsForEnrolledCourses()
    {
        // Arrange
        await SetupCoursesAndEnrollments();
        var generator = new LessonDataGenerator(_context, _seederContext);

        // Act
        var lessons = await generator.GenerateAsync(
            _seederContext.Courses, _seederContext.Enrollments, CancellationToken.None);

        // Assert
        Assert.NotEmpty(lessons);
    }

    [Fact]
    public async Task LessonDataGenerator_GenerateAsync_IndividualLessonsHaveStudentId()
    {
        // Arrange
        await SetupCoursesAndEnrollments();
        var generator = new LessonDataGenerator(_context, _seederContext);

        // Act
        var lessons = await generator.GenerateAsync(
            _seederContext.Courses, _seederContext.Enrollments, CancellationToken.None);

        var courseTypes = await _context.CourseTypes.ToDictionaryAsync(ct => ct.Id);
        var individualCourseIds = _seederContext.Courses
            .Where(c => courseTypes.TryGetValue(c.CourseTypeId, out var ct) &&
                       ct.Type == CourseTypeCategory.Individual)
            .Select(c => c.Id)
            .ToHashSet();

        var individualLessons = lessons.Where(l => individualCourseIds.Contains(l.CourseId)).ToList();

        // Assert
        Assert.All(individualLessons, l => Assert.NotNull(l.StudentId));
    }

    #endregion

    #region SupportDataGenerator Tests

    [Fact]
    public async Task SupportDataGenerator_GenerateHolidaysAsync_Creates11Holidays()
    {
        // Arrange
        var generator = new SupportDataGenerator(_context, _seederContext);

        // Act
        await generator.GenerateHolidaysAsync(CancellationToken.None);
        var holidays = await _context.Holidays.ToListAsync();

        // Assert
        Assert.Equal(11, holidays.Count);
    }

    [Fact]
    public async Task SupportDataGenerator_GenerateHolidaysAsync_IncludesDutchHolidays()
    {
        // Arrange
        var generator = new SupportDataGenerator(_context, _seederContext);

        // Act
        await generator.GenerateHolidaysAsync(CancellationToken.None);
        var holidays = await _context.Holidays.ToListAsync();

        // Assert
        Assert.Contains(holidays, h => h.Name == "Kerstvakantie");
        Assert.Contains(holidays, h => h.Name == "Zomervakantie");
        Assert.Contains(holidays, h => h.Name == "Koningsdag");
        Assert.Contains(holidays, h => h.Name == "Bevrijdingsdag");
    }

    [Fact]
    public async Task SupportDataGenerator_GenerateHolidaysAsync_IsIdempotent()
    {
        // Arrange
        var generator = new SupportDataGenerator(_context, _seederContext);
        await generator.GenerateHolidaysAsync(CancellationToken.None);

        // Act
        await generator.GenerateHolidaysAsync(CancellationToken.None);

        // Assert
        Assert.Equal(11, await _context.Holidays.CountAsync());
    }

    #endregion

    #region Helper Methods

    private async Task SetupCoursesAndEnrollments()
    {
        var teacherGenerator = new TeacherDataGenerator(_context, _seederContext);
        var studentGenerator = new StudentDataGenerator(_context, _seederContext);
        var courseGenerator = new CourseDataGenerator(_context, _seederContext);

        var teachers = await teacherGenerator.GenerateAsync(CancellationToken.None);
        var courseTypes = await courseGenerator.GenerateCourseTypesAsync(
            _seederContext.Instruments, CancellationToken.None);
        await courseGenerator.GeneratePricingVersionsAsync(courseTypes, CancellationToken.None);
        await teacherGenerator.GenerateCourseTypeLinksAsync(teachers, courseTypes, CancellationToken.None);
        var students = await studentGenerator.GenerateAsync(CancellationToken.None);
        var courses = await courseGenerator.GenerateCoursesAsync(
            courseTypes, _seederContext.Rooms, CancellationToken.None);
        await courseGenerator.GenerateEnrollmentsAsync(students, courses, CancellationToken.None);
    }

    #endregion
}
