using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Infrastructure.Repositories;

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
        Assert.NotEmpty(result);
        Assert.All(result, l =>
        {
            Assert.True(l.ScheduledDate >= startDate);
            Assert.True(l.ScheduledDate <= endDate);
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
        Assert.NotEmpty(result);
        var lesson = result.First();
        Assert.NotNull(lesson.Course);
        Assert.NotNull(lesson.Course.CourseType);
        Assert.NotNull(lesson.Course.CourseType.Instrument);
        Assert.NotNull(lesson.Student);
        Assert.NotNull(lesson.Teacher);
        Assert.NotNull(lesson.Room);
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
        Assert.NotEmpty(result);
        var list = result.ToList();
        for (int i = 0; i < list.Count - 1; i++)
        {
            var current = list[i];
            var next = list[i + 1];
            if (current.ScheduledDate == next.ScheduledDate)
            {
                Assert.True(current.StartTime <= next.StartTime);
            }
            else
            {
                Assert.True(current.ScheduledDate <= next.ScheduledDate);
            }
        }
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
        Assert.NotEmpty(result);
        Assert.All(result, l =>
        {
            Assert.Equal(teacher.Id, l.TeacherId);
            Assert.True(l.ScheduledDate >= startDate);
            Assert.True(l.ScheduledDate <= endDate);
        });
        Assert.NotNull(result.First().Course);
        Assert.NotNull(result.First().Student);
    }

    [Fact]
    public async Task GetByStudentAsync_ShouldReturnLessonsForSpecificStudent()
    {
        // Arrange
        var student = Context.Students.First();

        // Act
        var result = await _repository.GetByStudentAsync(student.Id);

        // Assert
        Assert.NotEmpty(result);
        Assert.All(result, l => Assert.Equal(student.Id, l.StudentId));
        Assert.NotNull(result.First().Course);
        Assert.NotNull(result.First().Course.CourseType);
        Assert.NotNull(result.First().Course.CourseType.Instrument);
        Assert.NotNull(result.First().Teacher);
        Assert.NotNull(result.First().Room);
    }

    [Fact]
    public async Task GetByStudentAsync_ShouldReturnLessonsOrderedByDateDescending()
    {
        // Arrange
        var student = Context.Students.First();

        // Act
        var result = await _repository.GetByStudentAsync(student.Id);

        // Assert
        Assert.NotEmpty(result);
        var list = result.ToList();
        for (int i = 0; i < list.Count - 1; i++)
        {
            var current = list[i];
            var next = list[i + 1];
            if (current.ScheduledDate == next.ScheduledDate)
            {
                Assert.True(current.StartTime >= next.StartTime);
            }
            else
            {
                Assert.True(current.ScheduledDate >= next.ScheduledDate);
            }
        }
    }

    [Fact]
    public async Task GetUninvoicedLessonsAsync_ShouldReturnOnlyCompletedUninvoicedLessons()
    {
        // Arrange
        var student = Context.Students.First();

        // Act
        var result = await _repository.GetUninvoicedLessonsAsync(student.Id);

        // Assert
        Assert.NotEmpty(result);
        Assert.All(result, l =>
        {
            Assert.Equal(student.Id, l.StudentId);
            Assert.False(l.IsInvoiced);
            Assert.Equal(LessonStatus.Completed, l.Status);
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
        Assert.DoesNotContain(result, l => l.Status == LessonStatus.Scheduled);
    }

    [Fact]
    public async Task GetUninvoicedLessonsAsync_ShouldNotReturnInvoicedLessons()
    {
        // Arrange
        var student = Context.Students.First();

        // Act
        var result = await _repository.GetUninvoicedLessonsAsync(student.Id);

        // Assert
        Assert.DoesNotContain(result, l => l.IsInvoiced);
    }

    [Fact]
    public async Task GetUninvoicedLessonsAsync_ShouldReturnLessonsOrderedByDateAscending()
    {
        // Arrange
        var student = Context.Students.First();

        // Act
        var result = await _repository.GetUninvoicedLessonsAsync(student.Id);

        // Assert
        Assert.NotEmpty(result);
        var list = result.ToList();
        for (int i = 0; i < list.Count - 1; i++)
        {
            var current = list[i];
            var next = list[i + 1];
            if (current.ScheduledDate == next.ScheduledDate)
            {
                Assert.True(current.StartTime <= next.StartTime);
            }
            else
            {
                Assert.True(current.ScheduledDate <= next.ScheduledDate);
            }
        }
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
        Assert.NotEmpty(result);
        Assert.All(result, l =>
        {
            Assert.Equal(room.Id, l.RoomId);
            Assert.Equal(date, l.ScheduledDate);
        });
        Assert.NotNull(result.First().Teacher);
        Assert.NotNull(result.First().Student);
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
        Assert.Equal(2, result.Count());
        var list = result.ToList();
        for (int i = 0; i < list.Count - 1; i++)
        {
            Assert.True(list[i].StartTime <= list[i + 1].StartTime);
        }
        Assert.Equal(new TimeOnly(10, 0), result.First().StartTime);
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
        Assert.Empty(result);
    }
}
