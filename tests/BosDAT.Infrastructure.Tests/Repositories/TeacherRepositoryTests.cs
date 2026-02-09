using BosDAT.Core.Entities;
using BosDAT.Infrastructure.Repositories;
using FluentAssertions;

namespace BosDAT.Infrastructure.Tests.Repositories;

public class TeacherRepositoryTests : RepositoryTestBase
{
    private readonly TeacherRepository _repository;

    public TeacherRepositoryTests()
    {
        _repository = new TeacherRepository(Context);
        SeedTestData();
    }

    [Fact]
    public async Task GetByEmailAsync_ShouldReturnTeacherWhenEmailExists()
    {
        // Arrange
        var expectedEmail = "john.doe@example.com";

        // Act
        var result = await _repository.GetByEmailAsync(expectedEmail);

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().BeEquivalentTo(expectedEmail);
    }

    [Fact]
    public async Task GetByEmailAsync_ShouldBeCaseInsensitive()
    {
        // Arrange
        var expectedEmail = "JOHN.DOE@EXAMPLE.COM";

        // Act
        var result = await _repository.GetByEmailAsync(expectedEmail);

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().BeEquivalentTo("john.doe@example.com");
    }

    [Fact]
    public async Task GetByEmailAsync_ShouldReturnNullWhenEmailDoesNotExist()
    {
        // Arrange
        var nonexistentEmail = "nonexistent@example.com";

        // Act
        var result = await _repository.GetByEmailAsync(nonexistentEmail);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetWithInstrumentsAsync_ShouldReturnTeacherWithInstruments()
    {
        // Arrange
        var teacher = Context.Teachers.First();
        var piano = Context.Instruments.First(i => i.Name == "Piano");

        var teacherInstrument = new TeacherInstrument
        {
            TeacherId = teacher.Id,
            InstrumentId = piano.Id
        };
        Context.Set<TeacherInstrument>().Add(teacherInstrument);
        await Context.SaveChangesAsync();

        // Act
        var result = await _repository.GetWithInstrumentsAsync(teacher.Id);

        // Assert
        result.Should().NotBeNull();
        result!.TeacherInstruments.Should().NotBeEmpty();
        result.TeacherInstruments.First().Instrument.Should().NotBeNull();
    }

    [Fact]
    public async Task GetWithInstrumentsAndCourseTypesAsync_ShouldReturnCompleteTeacherData()
    {
        // Arrange
        var teacher = Context.Teachers.First();
        var piano = Context.Instruments.First(i => i.Name == "Piano");
        var courseType = Context.CourseTypes.First();

        var teacherInstrument = new TeacherInstrument
        {
            TeacherId = teacher.Id,
            InstrumentId = piano.Id
        };

        var teacherCourseType = new TeacherCourseType
        {
            TeacherId = teacher.Id,
            CourseTypeId = courseType.Id
        };

        Context.Set<TeacherInstrument>().Add(teacherInstrument);
        Context.Set<TeacherCourseType>().Add(teacherCourseType);
        await Context.SaveChangesAsync();

        // Act
        var result = await _repository.GetWithInstrumentsAndCourseTypesAsync(teacher.Id);

        // Assert
        result.Should().NotBeNull();
        result!.TeacherInstruments.Should().NotBeEmpty();
        result.TeacherInstruments.First().Instrument.Should().NotBeNull();
        result.TeacherCourseTypes.Should().NotBeEmpty();
        result.TeacherCourseTypes.First().CourseType.Should().NotBeNull();
        result.TeacherCourseTypes.First().CourseType.Instrument.Should().NotBeNull();
    }

    [Fact]
    public async Task GetWithCoursesAsync_ShouldReturnTeacherWithAllCourseData()
    {
        // Arrange
        var teacher = Context.Teachers.First(t => t.Email == "john.doe@example.com");

        // Act
        var result = await _repository.GetWithCoursesAsync(teacher.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Courses.Should().NotBeEmpty();
        result.Courses.First().CourseType.Should().NotBeNull();
        result.Courses.First().CourseType.Instrument.Should().NotBeNull();
        result.Courses.First().Room.Should().NotBeNull();
        result.Courses.First().Enrollments.Should().NotBeNull();
    }

    [Fact]
    public async Task GetActiveTeachersAsync_ShouldReturnOnlyActiveTeachers()
    {
        // Arrange
        var inactiveTeacher = new Teacher
        {
            Id = Guid.NewGuid(),
            FirstName = "Inactive",
            LastName = "Teacher",
            Email = "inactive@example.com",
            Phone = "0600000000",
            IsActive = false
        };
        Context.Teachers.Add(inactiveTeacher);
        await Context.SaveChangesAsync();

        // Act
        var result = await _repository.GetActiveTeachersAsync();

        // Assert
        result.Should().NotBeEmpty();
        result.Should().AllSatisfy(t => t.IsActive.Should().BeTrue());
        result.Should().NotContain(t => t.Id == inactiveTeacher.Id);
    }

    [Fact]
    public async Task GetActiveTeachersAsync_ShouldReturnTeachersOrderedByLastNameThenFirstName()
    {
        // Arrange
        var teacher1 = new Teacher
        {
            Id = Guid.NewGuid(),
            FirstName = "Zack",
            LastName = "Anderson",
            Email = "zack.anderson@example.com",
            Phone = "0600000001",
            IsActive = true
        };

        var teacher2 = new Teacher
        {
            Id = Guid.NewGuid(),
            FirstName = "Aaron",
            LastName = "Anderson",
            Email = "aaron.anderson@example.com",
            Phone = "0600000002",
            IsActive = true
        };

        Context.Teachers.AddRange(teacher1, teacher2);
        await Context.SaveChangesAsync();

        // Act
        var result = await _repository.GetActiveTeachersAsync();

        // Assert
        var andersons = result.Where(t => t.LastName == "Anderson").ToList();
        andersons.Should().HaveCountGreaterThanOrEqualTo(2);
        andersons.Should().BeInAscendingOrder(t => t.FirstName);
    }

    [Fact]
    public async Task GetTeachersByInstrumentAsync_ShouldReturnTeachersWhoTeachThatInstrument()
    {
        // Arrange
        var teacher = Context.Teachers.First();
        var piano = Context.Instruments.First(i => i.Name == "Piano");

        var teacherInstrument = new TeacherInstrument
        {
            TeacherId = teacher.Id,
            InstrumentId = piano.Id
        };
        Context.Set<TeacherInstrument>().Add(teacherInstrument);
        await Context.SaveChangesAsync();

        // Act
        var result = await _repository.GetTeachersByInstrumentAsync(piano.Id);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().AllSatisfy(t =>
        {
            t.IsActive.Should().BeTrue();
            t.TeacherInstruments.Should().Contain(ti => ti.InstrumentId == piano.Id);
        });
    }

    [Fact]
    public async Task GetTeachersByInstrumentAsync_ShouldNotReturnInactiveTeachers()
    {
        // Arrange
        var inactiveTeacher = new Teacher
        {
            Id = Guid.NewGuid(),
            FirstName = "Inactive",
            LastName = "Piano",
            Email = "inactivepiano@example.com",
            Phone = "0600000000",
            IsActive = false
        };

        var piano = Context.Instruments.First(i => i.Name == "Piano");

        var teacherInstrument = new TeacherInstrument
        {
            TeacherId = inactiveTeacher.Id,
            InstrumentId = piano.Id
        };

        Context.Teachers.Add(inactiveTeacher);
        Context.Set<TeacherInstrument>().Add(teacherInstrument);
        await Context.SaveChangesAsync();

        // Act
        var result = await _repository.GetTeachersByInstrumentAsync(piano.Id);

        // Assert
        result.Should().NotContain(t => t.Id == inactiveTeacher.Id);
    }

    [Fact]
    public async Task GetTeachersByCourseTypeAsync_ShouldReturnTeachersWhoTeachThatCourseType()
    {
        // Arrange
        var teacher = Context.Teachers.First();
        var courseType = Context.CourseTypes.First();

        var teacherCourseType = new TeacherCourseType
        {
            TeacherId = teacher.Id,
            CourseTypeId = courseType.Id
        };
        Context.Set<TeacherCourseType>().Add(teacherCourseType);
        await Context.SaveChangesAsync();

        // Act
        var result = await _repository.GetTeachersByCourseTypeAsync(courseType.Id);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().AllSatisfy(t =>
        {
            t.IsActive.Should().BeTrue();
            t.TeacherCourseTypes.Should().Contain(tct => tct.CourseTypeId == courseType.Id);
        });
        result.First().TeacherInstruments.Should().NotBeNull();
        result.First().TeacherCourseTypes.First().CourseType.Should().NotBeNull();
    }

    [Fact]
    public async Task GetWithAvailabilityAsync_ShouldReturnTeacherWithAvailability()
    {
        // Arrange
        var teacher = Context.Teachers.First();

        var availability = new TeacherAvailability
        {
            Id = Guid.NewGuid(),
            TeacherId = teacher.Id,
            DayOfWeek = DayOfWeek.Monday,
            FromTime = new TimeOnly(9, 0),
            UntilTime = new TimeOnly(17, 0)
        };
        Context.Set<TeacherAvailability>().Add(availability);
        await Context.SaveChangesAsync();

        // Act
        var result = await _repository.GetWithAvailabilityAsync(teacher.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Availability.Should().NotBeEmpty();
        result.Availability.First().DayOfWeek.Should().Be(DayOfWeek.Monday);
    }

    [Fact]
    public async Task GetAvailabilityAsync_ShouldReturnAvailabilityOrderedByDayOfWeek()
    {
        // Arrange
        var teacher = Context.Teachers.First();

        var monday = new TeacherAvailability
        {
            Id = Guid.NewGuid(),
            TeacherId = teacher.Id,
            DayOfWeek = DayOfWeek.Monday,
            FromTime = new TimeOnly(9, 0),
            UntilTime = new TimeOnly(17, 0)
        };

        var wednesday = new TeacherAvailability
        {
            Id = Guid.NewGuid(),
            TeacherId = teacher.Id,
            DayOfWeek = DayOfWeek.Wednesday,
            FromTime = new TimeOnly(9, 0),
            UntilTime = new TimeOnly(17, 0)
        };

        Context.Set<TeacherAvailability>().AddRange(monday, wednesday);
        await Context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAvailabilityAsync(teacher.Id);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().BeInAscendingOrder(a => a.DayOfWeek);
        result.First().DayOfWeek.Should().Be(DayOfWeek.Monday);
    }

    [Fact]
    public async Task GetAvailabilityAsync_ShouldReturnEmptyForTeacherWithNoAvailability()
    {
        // Arrange
        var teacherWithNoAvailability = new Teacher
        {
            Id = Guid.NewGuid(),
            FirstName = "No",
            LastName = "Availability",
            Email = "noavailability@example.com",
            Phone = "0600000000",
            IsActive = true
        };
        Context.Teachers.Add(teacherWithNoAvailability);
        await Context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAvailabilityAsync(teacherWithNoAvailability.Id);

        // Assert
        result.Should().BeEmpty();
    }
}
