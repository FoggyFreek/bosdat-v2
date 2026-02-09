using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Infrastructure.Repositories;
using FluentAssertions;

namespace BosDAT.Infrastructure.Tests.Repositories;

public class LessonRepositoryTests : RepositoryTestBase
{
    private readonly LessonRepository _repository;

    public LessonRepositoryTests()
    {
        _repository = new LessonRepository(Context);
        SeedTestData();
        SeedLessons();
    }

    private void SeedLessons()
    {
        var course = Context.Courses.First();
        var student = Context.Students.First();
        var teacher = Context.Teachers.First();
        var room = Context.Rooms.First();

        var lesson1 = new Lesson
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            StudentId = student.Id,
            TeacherId = teacher.Id,
            RoomId = room.Id,
            ScheduledDate = new DateOnly(2024, 2, 5),
            StartTime = new TimeOnly(14, 0),
            EndTime = new TimeOnly(14, 30),
            Status = LessonStatus.Scheduled,
            IsInvoiced = false
        };

        var lesson2 = new Lesson
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            StudentId = student.Id,
            TeacherId = teacher.Id,
            RoomId = room.Id,
            ScheduledDate = new DateOnly(2024, 2, 12),
            StartTime = new TimeOnly(14, 0),
            EndTime = new TimeOnly(14, 30),
            Status = LessonStatus.Completed,
            IsInvoiced = false
        };

        var lesson3 = new Lesson
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            StudentId = student.Id,
            TeacherId = teacher.Id,
            RoomId = room.Id,
            ScheduledDate = new DateOnly(2024, 3, 1),
            StartTime = new TimeOnly(14, 0),
            EndTime = new TimeOnly(14, 30),
            Status = LessonStatus.Completed,
            IsInvoiced = true
        };

        Context.Lessons.AddRange(lesson1, lesson2, lesson3);
        Context.SaveChanges();
    }

    [Fact]
    public async Task GetByDateRangeAsync_ShouldReturnLessonsWithinRange()
    {
        // Arrange
        var startDate = new DateOnly(2024, 2, 1);
        var endDate = new DateOnly(2024, 2, 28);

        // Act
        var result = await _repository.GetByDateRangeAsync(startDate, endDate);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().AllSatisfy(l =>
        {
            l.ScheduledDate.Should().BeOnOrAfter(startDate);
            l.ScheduledDate.Should().BeOnOrBefore(endDate);
        });
    }

    [Fact]
    public async Task GetByDateRangeAsync_ShouldIncludeAllRelatedData()
    {
        // Arrange
        var startDate = new DateOnly(2024, 2, 1);
        var endDate = new DateOnly(2024, 2, 28);

        // Act
        var result = await _repository.GetByDateRangeAsync(startDate, endDate);

        // Assert
        result.Should().NotBeEmpty();
        var lesson = result.First();
        lesson.Course.Should().NotBeNull();
        lesson.Course.CourseType.Should().NotBeNull();
        lesson.Course.CourseType.Instrument.Should().NotBeNull();
        lesson.Student.Should().NotBeNull();
        lesson.Teacher.Should().NotBeNull();
        lesson.Room.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByDateRangeAsync_ShouldReturnLessonsOrderedByDateAndTime()
    {
        // Arrange
        var course = Context.Courses.First();
        var student = Context.Students.First();
        var teacher = Context.Teachers.First();
        var room = Context.Rooms.First();

        var lesson1 = new Lesson
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            StudentId = student.Id,
            TeacherId = teacher.Id,
            RoomId = room.Id,
            ScheduledDate = new DateOnly(2024, 4, 1),
            StartTime = new TimeOnly(15, 0),
            EndTime = new TimeOnly(15, 30),
            Status = LessonStatus.Scheduled,
            IsInvoiced = false
        };

        var lesson2 = new Lesson
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            StudentId = student.Id,
            TeacherId = teacher.Id,
            RoomId = room.Id,
            ScheduledDate = new DateOnly(2024, 4, 1),
            StartTime = new TimeOnly(14, 0),
            EndTime = new TimeOnly(14, 30),
            Status = LessonStatus.Scheduled,
            IsInvoiced = false
        };

        Context.Lessons.AddRange(lesson1, lesson2);
        await Context.SaveChangesAsync();

        var startDate = new DateOnly(2024, 4, 1);
        var endDate = new DateOnly(2024, 4, 30);

        // Act
        var result = await _repository.GetByDateRangeAsync(startDate, endDate);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().BeInAscendingOrder(l => l.ScheduledDate).And.ThenBeInAscendingOrder(l => l.StartTime);
    }

    [Fact]
    public async Task GetByTeacherAndDateRangeAsync_ShouldReturnLessonsForSpecificTeacher()
    {
        // Arrange
        var teacher = Context.Teachers.First();
        var startDate = new DateOnly(2024, 2, 1);
        var endDate = new DateOnly(2024, 2, 28);

        // Act
        var result = await _repository.GetByTeacherAndDateRangeAsync(teacher.Id, startDate, endDate);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().AllSatisfy(l =>
        {
            l.TeacherId.Should().Be(teacher.Id);
            l.ScheduledDate.Should().BeOnOrAfter(startDate);
            l.ScheduledDate.Should().BeOnOrBefore(endDate);
        });
        result.First().Course.Should().NotBeNull();
        result.First().Student.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByStudentAsync_ShouldReturnLessonsForSpecificStudent()
    {
        // Arrange
        var student = Context.Students.First();

        // Act
        var result = await _repository.GetByStudentAsync(student.Id);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().AllSatisfy(l => l.StudentId.Should().Be(student.Id));
        result.First().Course.Should().NotBeNull();
        result.First().Course.CourseType.Should().NotBeNull();
        result.First().Course.CourseType.Instrument.Should().NotBeNull();
        result.First().Teacher.Should().NotBeNull();
        result.First().Room.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByStudentAsync_ShouldReturnLessonsOrderedByDateDescending()
    {
        // Arrange
        var student = Context.Students.First();

        // Act
        var result = await _repository.GetByStudentAsync(student.Id);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().BeInDescendingOrder(l => l.ScheduledDate)
            .And.ThenBeInDescendingOrder(l => l.StartTime);
    }

    [Fact]
    public async Task GetUninvoicedLessonsAsync_ShouldReturnOnlyCompletedUninvoicedLessons()
    {
        // Arrange
        var student = Context.Students.First();

        // Act
        var result = await _repository.GetUninvoicedLessonsAsync(student.Id);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().AllSatisfy(l =>
        {
            l.StudentId.Should().Be(student.Id);
            l.IsInvoiced.Should().BeFalse();
            l.Status.Should().Be(LessonStatus.Completed);
        });
    }

    [Fact]
    public async Task GetUninvoicedLessonsAsync_ShouldNotReturnScheduledLessons()
    {
        // Arrange
        var student = Context.Students.First();
        var lessons = Context.Lessons.Where(l => l.StudentId == student.Id && l.Status == LessonStatus.Scheduled);

        // Act
        var result = await _repository.GetUninvoicedLessonsAsync(student.Id);

        // Assert
        result.Should().NotContain(l => l.Status == LessonStatus.Scheduled);
    }

    [Fact]
    public async Task GetUninvoicedLessonsAsync_ShouldNotReturnInvoicedLessons()
    {
        // Arrange
        var student = Context.Students.First();

        // Act
        var result = await _repository.GetUninvoicedLessonsAsync(student.Id);

        // Assert
        result.Should().NotContain(l => l.IsInvoiced);
    }

    [Fact]
    public async Task GetUninvoicedLessonsAsync_ShouldReturnLessonsOrderedByDateAscending()
    {
        // Arrange
        var student = Context.Students.First();

        // Act
        var result = await _repository.GetUninvoicedLessonsAsync(student.Id);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().BeInAscendingOrder(l => l.ScheduledDate)
            .And.ThenBeInAscendingOrder(l => l.StartTime);
    }

    [Fact]
    public async Task GetByRoomAndDateAsync_ShouldReturnLessonsForSpecificRoomAndDate()
    {
        // Arrange
        var room = Context.Rooms.First();
        var date = new DateOnly(2024, 2, 5);

        // Act
        var result = await _repository.GetByRoomAndDateAsync(room.Id, date);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().AllSatisfy(l =>
        {
            l.RoomId.Should().Be(room.Id);
            l.ScheduledDate.Should().Be(date);
        });
        result.First().Teacher.Should().NotBeNull();
        result.First().Student.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByRoomAndDateAsync_ShouldReturnLessonsOrderedByStartTime()
    {
        // Arrange
        var room = Context.Rooms.First();
        var date = new DateOnly(2024, 5, 1);
        var course = Context.Courses.First();
        var student = Context.Students.First();
        var teacher = Context.Teachers.First();

        var lesson1 = new Lesson
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            StudentId = student.Id,
            TeacherId = teacher.Id,
            RoomId = room.Id,
            ScheduledDate = date,
            StartTime = new TimeOnly(15, 0),
            EndTime = new TimeOnly(15, 30),
            Status = LessonStatus.Scheduled,
            IsInvoiced = false
        };

        var lesson2 = new Lesson
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            StudentId = student.Id,
            TeacherId = teacher.Id,
            RoomId = room.Id,
            ScheduledDate = date,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(10, 30),
            Status = LessonStatus.Scheduled,
            IsInvoiced = false
        };

        Context.Lessons.AddRange(lesson1, lesson2);
        await Context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByRoomAndDateAsync(room.Id, date);

        // Assert
        result.Should().HaveCount(2);
        result.Should().BeInAscendingOrder(l => l.StartTime);
        result.First().StartTime.Should().Be(new TimeOnly(10, 0));
    }

    [Fact]
    public async Task GetByRoomAndDateAsync_ShouldReturnEmptyForNoLessons()
    {
        // Arrange
        var room = Context.Rooms.First();
        var date = new DateOnly(2025, 12, 31);

        // Act
        var result = await _repository.GetByRoomAndDateAsync(room.Id, date);

        // Assert
        result.Should().BeEmpty();
    }
}
