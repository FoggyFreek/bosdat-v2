using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Infrastructure.Repositories;
using FluentAssertions;

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
        result.Should().NotBeNull();
        result!.Id.Should().Be(courseId);
        result.Teacher.Should().NotBeNull();
        result.CourseType.Should().NotBeNull();
        result.CourseType.Instrument.Should().NotBeNull();
        result.Room.Should().NotBeNull();
        result.Enrollments.Should().NotBeEmpty();
        result.Enrollments.First().Student.Should().NotBeNull();
    }

    [Fact]
    public async Task GetWithEnrollmentsAsync_ShouldReturnNullForNonexistentCourse()
    {
        // Arrange
        var nonexistentId = Guid.NewGuid();

        // Act
        var result = await _repository.GetWithEnrollmentsAsync(nonexistentId);

        // Assert
        result.Should().BeNull();
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
        result.Should().NotBeEmpty();
        result.Should().AllSatisfy(c => c.TeacherId.Should().Be(teacherId));
        result.First().CourseType.Should().NotBeNull();
        result.First().CourseType.Instrument.Should().NotBeNull();
        result.First().Room.Should().NotBeNull();
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
        result.Should().BeEmpty();
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
        result.Should().HaveCountGreaterThan(1);
        var tuesdayCourses = result.Where(c => c.DayOfWeek == DayOfWeek.Tuesday).ToList();
        tuesdayCourses.Should().BeInAscendingOrder(c => c.StartTime);
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
        result.Should().NotBeEmpty();
        result.Should().AllSatisfy(c => c.Status.Should().Be(CourseStatus.Active));
        result.Should().NotContain(c => c.Id == inactiveCourse.Id);
        result.First().Teacher.Should().NotBeNull();
        result.First().CourseType.Should().NotBeNull();
        result.First().Room.Should().NotBeNull();
    }

    [Fact]
    public async Task GetCoursesByDayAsync_ShouldReturnCoursesForSpecificDay()
    {
        // Arrange
        var targetDay = DayOfWeek.Monday;

        // Act
        var result = await _repository.GetCoursesByDayAsync(targetDay);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().AllSatisfy(c =>
        {
            c.DayOfWeek.Should().Be(targetDay);
            c.Status.Should().Be(CourseStatus.Active);
        });
        result.First().Teacher.Should().NotBeNull();
        result.First().CourseType.Should().NotBeNull();
        result.First().Room.Should().NotBeNull();
        result.First().Enrollments.Should().NotBeNull();
    }

    [Fact]
    public async Task GetCoursesByDayAsync_ShouldReturnEmptyForDayWithNoCourses()
    {
        // Arrange
        var dayWithNoCourses = DayOfWeek.Saturday;

        // Act
        var result = await _repository.GetCoursesByDayAsync(dayWithNoCourses);

        // Assert
        result.Should().BeEmpty();
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
        result.Should().HaveCount(2);
        result.Should().BeInAscendingOrder(c => c.StartTime);
        result.First().StartTime.Should().Be(new TimeOnly(10, 0));
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
        result.Should().NotContain(c => c.Id == inactiveCourse.Id);
    }
}
