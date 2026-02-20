using BosDAT.Core.Entities;
using BosDAT.Infrastructure.Repositories;

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
        Assert.NotNull(result);
        Assert.Equal(expectedEmail.ToLower(), result!.Email.ToLower());
    }

    [Fact]
    public async Task GetByEmailAsync_ShouldBeCaseInsensitive()
    {
        // Arrange
        var expectedEmail = "JOHN.DOE@EXAMPLE.COM";

        // Act
        var result = await _repository.GetByEmailAsync(expectedEmail);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("john.doe@example.com".ToLower(), result!.Email.ToLower());
    }

    [Fact]
    public async Task GetByEmailAsync_ShouldReturnNullWhenEmailDoesNotExist()
    {
        // Arrange
        var nonexistentEmail = "nonexistent@example.com";

        // Act
        var result = await _repository.GetByEmailAsync(nonexistentEmail);

        // Assert
        Assert.Null(result);
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
        Assert.NotNull(result);
        Assert.NotEmpty(result!.TeacherInstruments);
        Assert.NotNull(result.TeacherInstruments.First().Instrument);
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
        Assert.NotNull(result);
        Assert.NotEmpty(result!.TeacherInstruments);
        Assert.NotNull(result.TeacherInstruments.First().Instrument);
        Assert.NotEmpty(result.TeacherCourseTypes);
        Assert.NotNull(result.TeacherCourseTypes.First().CourseType);
        Assert.NotNull(result.TeacherCourseTypes.First().CourseType.Instrument);
    }

    [Fact]
    public async Task GetWithCoursesAsync_ShouldReturnTeacherWithAllCourseData()
    {
        // Arrange
        var teacher = Context.Teachers.First(t => t.Email == "john.doe@example.com");

        // Act
        var result = await _repository.GetWithCoursesAsync(teacher.Id);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result!.Courses);
        Assert.NotNull(result.Courses.First().CourseType);
        Assert.NotNull(result.Courses.First().CourseType.Instrument);
        Assert.NotNull(result.Courses.First().Room);
        Assert.NotNull(result.Courses.First().Enrollments);
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
        Assert.NotEmpty(result);
        Assert.All(result, t => Assert.True(t.IsActive));
        Assert.DoesNotContain(result, t => t.Id == inactiveTeacher.Id);
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
        Assert.True(andersons.Count >= 2);
        for (int i = 0; i < andersons.Count - 1; i++)
        {
            Assert.True(string.Compare(andersons[i].FirstName, andersons[i + 1].FirstName, StringComparison.Ordinal) <= 0);
        }
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
        Assert.NotEmpty(result);
        Assert.All(result, t =>
        {
            Assert.True(t.IsActive);
            Assert.Contains(t.TeacherInstruments, ti => ti.InstrumentId == piano.Id);
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
        Assert.DoesNotContain(result, t => t.Id == inactiveTeacher.Id);
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
        Assert.NotEmpty(result);
        Assert.All(result, t =>
        {
            Assert.True(t.IsActive);
            Assert.Contains(t.TeacherCourseTypes, tct => tct.CourseTypeId == courseType.Id);
        });
        Assert.NotNull(result[0].TeacherInstruments);
        Assert.NotNull(result[0].TeacherCourseTypes.First().CourseType);
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
        Assert.NotNull(result);
        Assert.NotEmpty(result!.Availability);
        Assert.Equal(DayOfWeek.Monday, result.Availability.First().DayOfWeek);
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
        Assert.NotEmpty(result);
        var list = result.ToList();
        for (int i = 0; i < list.Count - 1; i++)
        {
            Assert.True(list[i].DayOfWeek <= list[i + 1].DayOfWeek);
        }
        Assert.Equal(DayOfWeek.Monday, result[0].DayOfWeek);
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
        Assert.Empty(result);
    }
}
