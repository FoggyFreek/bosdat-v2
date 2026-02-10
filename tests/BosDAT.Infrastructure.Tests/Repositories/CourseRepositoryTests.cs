using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Infrastructure.Repositories;

namespace BosDAT.Infrastructure.Tests.Repositories;

public class CourseRepositoryTests : RepositoryTestBase
{
    private readonly CourseRepository _repository;

    public CourseRepositoryTests()
    {
        _repository = new CourseRepository(Context);
        SeedTestData();
    }

    [Fact]
    public async Task GetWithEnrollmentsAsync_ShouldReturnCourseWithAllRelatedData()
    {
        // Arrange
        var course = Context.Courses.First();
        var courseId = course.Id;

        // Act
        var result = await _repository.GetWithEnrollmentsAsync(courseId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(courseId, result!.Id);
        Assert.NotNull(result.Teacher);
        Assert.NotNull(result.CourseType);
        Assert.NotNull(result.CourseType.Instrument);
        Assert.NotNull(result.Room);
        Assert.NotEmpty(result.Enrollments);
        Assert.NotNull(result.Enrollments.First().Student);
    }

    [Fact]
    public async Task GetWithEnrollmentsAsync_ShouldReturnNullForNonexistentCourse()
    {
        // Arrange
        var nonexistentId = Guid.NewGuid();

        // Act
        var result = await _repository.GetWithEnrollmentsAsync(nonexistentId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByTeacherAsync_ShouldReturnCoursesForSpecificTeacher()
    {
        // Arrange
        var teacher = Context.Teachers.First();
        var teacherId = teacher.Id;

        // Act
        var result = await _repository.GetByTeacherAsync(teacherId);

        // Assert
        Assert.NotEmpty(result);
        Assert.All(result, c => Assert.Equal(teacherId, c.TeacherId));
        Assert.NotNull(result.First().CourseType);
        Assert.NotNull(result.First().CourseType.Instrument);
        Assert.NotNull(result.First().Room);
    }

    [Fact]
    public async Task GetByTeacherAsync_ShouldReturnEmptyForTeacherWithNoCourses()
    {
        // Arrange
        var teacherWithNoCourses = new Teacher
        {
            Id = Guid.NewGuid(),
            FirstName = "No",
            LastName = "Courses",
            Email = "nocourses@example.com",
            Phone = "0600000000",
            IsActive = true
        };
        Context.Teachers.Add(teacherWithNoCourses);
        await Context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByTeacherAsync(teacherWithNoCourses.Id);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByTeacherAsync_ShouldReturnCoursesOrderedByDayAndTime()
    {
        // Arrange
        var teacher = Context.Teachers.First();
        var teacherId = teacher.Id;

        // Create additional courses with different times
        var courseType = Context.CourseTypes.First();
        var room = Context.Rooms.First();

        var course1 = new Course
        {
            Id = Guid.NewGuid(),
            CourseTypeId = courseType.Id,
            TeacherId = teacherId,
            RoomId = room.Id,
            DayOfWeek = DayOfWeek.Tuesday,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0),
            StartDate = new DateOnly(2024, 1, 1),
            EndDate = new DateOnly(2024, 12, 31),
            Status = CourseStatus.Active,
            Frequency = CourseFrequency.Weekly
        };

        var course2 = new Course
        {
            Id = Guid.NewGuid(),
            CourseTypeId = courseType.Id,
            TeacherId = teacherId,
            RoomId = room.Id,
            DayOfWeek = DayOfWeek.Tuesday,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 0),
            StartDate = new DateOnly(2024, 1, 1),
            EndDate = new DateOnly(2024, 12, 31),
            Status = CourseStatus.Active,
            Frequency = CourseFrequency.Weekly
        };

        Context.Courses.AddRange(course1, course2);
        await Context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByTeacherAsync(teacherId);

        // Assert
        Assert.True(result.Count() > 1);
        var tuesdayCourses = result.Where(c => c.DayOfWeek == DayOfWeek.Tuesday).ToList();
        for (int i = 0; i < tuesdayCourses.Count - 1; i++)
        {
            Assert.True(tuesdayCourses[i].StartTime <= tuesdayCourses[i + 1].StartTime);
        }
    }

    [Fact]
    public async Task GetActiveCoursesAsync_ShouldReturnOnlyActiveCourses()
    {
        // Arrange
        var courseType = Context.CourseTypes.First();
        var teacher = Context.Teachers.First();
        var room = Context.Rooms.First();

        var inactiveCourse = new Course
        {
            Id = Guid.NewGuid(),
            CourseTypeId = courseType.Id,
            TeacherId = teacher.Id,
            RoomId = room.Id,
            DayOfWeek = DayOfWeek.Friday,
            StartTime = new TimeOnly(15, 0),
            EndTime = new TimeOnly(16, 0),
            StartDate = new DateOnly(2024, 1, 1),
            EndDate = new DateOnly(2024, 12, 31),
            Status = CourseStatus.Completed,
            Frequency = CourseFrequency.Weekly
        };

        Context.Courses.Add(inactiveCourse);
        await Context.SaveChangesAsync();

        // Act
        var result = await _repository.GetActiveCoursesAsync();

        // Assert
        Assert.NotEmpty(result);
        Assert.All(result, c => Assert.Equal(CourseStatus.Active, c.Status));
        Assert.DoesNotContain(result, c => c.Id == inactiveCourse.Id);
        Assert.NotNull(result.First().Teacher);
        Assert.NotNull(result.First().CourseType);
        Assert.NotNull(result.First().Room);
    }

    [Fact]
    public async Task GetCoursesByDayAsync_ShouldReturnCoursesForSpecificDay()
    {
        // Arrange
        var targetDay = DayOfWeek.Monday;

        // Act
        var result = await _repository.GetCoursesByDayAsync(targetDay);

        // Assert
        Assert.NotEmpty(result);
        Assert.All(result, c =>
        {
            Assert.Equal(targetDay, c.DayOfWeek);
            Assert.Equal(CourseStatus.Active, c.Status);
        });
        Assert.NotNull(result.First().Teacher);
        Assert.NotNull(result.First().CourseType);
        Assert.NotNull(result.First().Room);
        Assert.NotNull(result.First().Enrollments);
    }

    [Fact]
    public async Task GetCoursesByDayAsync_ShouldReturnEmptyForDayWithNoCourses()
    {
        // Arrange
        var dayWithNoCourses = DayOfWeek.Saturday;

        // Act
        var result = await _repository.GetCoursesByDayAsync(dayWithNoCourses);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetCoursesByDayAsync_ShouldReturnCoursesOrderedByStartTime()
    {
        // Arrange
        var targetDay = DayOfWeek.Thursday;
        var courseType = Context.CourseTypes.First();
        var teacher = Context.Teachers.First();
        var room = Context.Rooms.First();

        var course1 = new Course
        {
            Id = Guid.NewGuid(),
            CourseTypeId = courseType.Id,
            TeacherId = teacher.Id,
            RoomId = room.Id,
            DayOfWeek = targetDay,
            StartTime = new TimeOnly(14, 0),
            EndTime = new TimeOnly(15, 0),
            StartDate = new DateOnly(2024, 1, 1),
            EndDate = new DateOnly(2024, 12, 31),
            Status = CourseStatus.Active,
            Frequency = CourseFrequency.Weekly
        };

        var course2 = new Course
        {
            Id = Guid.NewGuid(),
            CourseTypeId = courseType.Id,
            TeacherId = teacher.Id,
            RoomId = room.Id,
            DayOfWeek = targetDay,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0),
            StartDate = new DateOnly(2024, 1, 1),
            EndDate = new DateOnly(2024, 12, 31),
            Status = CourseStatus.Active,
            Frequency = CourseFrequency.Weekly
        };

        Context.Courses.AddRange(course1, course2);
        await Context.SaveChangesAsync();

        // Act
        var result = await _repository.GetCoursesByDayAsync(targetDay);

        // Assert
        Assert.Equal(2, result.Count());
        var list = result.ToList();
        for (int i = 0; i < list.Count - 1; i++)
        {
            Assert.True(list[i].StartTime <= list[i + 1].StartTime);
        }
        Assert.Equal(new TimeOnly(10, 0), result.First().StartTime);
    }

    [Fact]
    public async Task GetCoursesByDayAsync_ShouldNotReturnInactiveCourses()
    {
        // Arrange
        var targetDay = DayOfWeek.Friday;
        var courseType = Context.CourseTypes.First();
        var teacher = Context.Teachers.First();
        var room = Context.Rooms.First();

        var inactiveCourse = new Course
        {
            Id = Guid.NewGuid(),
            CourseTypeId = courseType.Id,
            TeacherId = teacher.Id,
            RoomId = room.Id,
            DayOfWeek = targetDay,
            StartTime = new TimeOnly(14, 0),
            EndTime = new TimeOnly(15, 0),
            StartDate = new DateOnly(2024, 1, 1),
            EndDate = new DateOnly(2024, 12, 31),
            Status = CourseStatus.Cancelled,
            Frequency = CourseFrequency.Weekly
        };

        Context.Courses.Add(inactiveCourse);
        await Context.SaveChangesAsync();

        // Act
        var result = await _repository.GetCoursesByDayAsync(targetDay);

        // Assert
        Assert.DoesNotContain(result, c => c.Id == inactiveCourse.Id);
    }
}
