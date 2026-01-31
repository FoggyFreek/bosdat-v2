using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Core.Interfaces;
using BosDAT.Infrastructure.Services;
using Moq;
using Xunit;

namespace BosDAT.API.Tests.Services;

public class ScheduleConflictServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly IScheduleConflictService _service;

    public ScheduleConflictServiceTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _service = new ScheduleConflictService(_mockUnitOfWork.Object);
    }

    [Fact]
    public async Task HasConflictAsync_NoExistingEnrollments_ShouldReturnNoConflict()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var courseId = Guid.NewGuid();

        var targetCourse = CreateCourse(courseId, DayOfWeek.Monday, new TimeOnly(10, 0), new TimeOnly(11, 30));

        _mockUnitOfWork.Setup(u => u.Courses.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetCourse);

        _mockUnitOfWork.Setup(u => u.Enrollments.GetActiveEnrollmentsByStudentIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Enrollment>());

        // Act
        var result = await _service.HasConflictAsync(studentId, courseId);

        // Assert
        Assert.False(result.HasConflict);
        Assert.Empty(result.ConflictingCourses);
    }

    [Fact]
    public async Task HasConflictAsync_DifferentDays_ShouldReturnNoConflict()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var targetCourseId = Guid.NewGuid();
        var existingCourseId = Guid.NewGuid();

        var targetCourse = CreateCourse(targetCourseId, DayOfWeek.Monday, new TimeOnly(10, 0), new TimeOnly(11, 30));
        var existingCourse = CreateCourse(existingCourseId, DayOfWeek.Tuesday, new TimeOnly(10, 0), new TimeOnly(11, 30));

        var existingEnrollment = CreateEnrollment(studentId, existingCourse);

        _mockUnitOfWork.Setup(u => u.Courses.GetByIdAsync(targetCourseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetCourse);

        _mockUnitOfWork.Setup(u => u.Enrollments.GetActiveEnrollmentsByStudentIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Enrollment> { existingEnrollment });

        // Act
        var result = await _service.HasConflictAsync(studentId, targetCourseId);

        // Assert
        Assert.False(result.HasConflict);
        Assert.Empty(result.ConflictingCourses);
    }

    [Fact]
    public async Task HasConflictAsync_SameDayNonOverlappingTimes_ShouldReturnNoConflict()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var targetCourseId = Guid.NewGuid();
        var existingCourseId = Guid.NewGuid();

        var targetCourse = CreateCourse(targetCourseId, DayOfWeek.Monday, new TimeOnly(10, 0), new TimeOnly(11, 30));
        var existingCourse = CreateCourse(existingCourseId, DayOfWeek.Monday, new TimeOnly(12, 0), new TimeOnly(13, 30));

        var existingEnrollment = CreateEnrollment(studentId, existingCourse);

        _mockUnitOfWork.Setup(u => u.Courses.GetByIdAsync(targetCourseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetCourse);

        _mockUnitOfWork.Setup(u => u.Enrollments.GetActiveEnrollmentsByStudentIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Enrollment> { existingEnrollment });

        // Act
        var result = await _service.HasConflictAsync(studentId, targetCourseId);

        // Assert
        Assert.False(result.HasConflict);
        Assert.Empty(result.ConflictingCourses);
    }

    [Fact]
    public async Task HasConflictAsync_WeeklyCoursesSameTimeslot_ShouldReturnConflict()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var targetCourseId = Guid.NewGuid();
        var existingCourseId = Guid.NewGuid();

        var targetCourse = CreateCourse(targetCourseId, DayOfWeek.Monday, new TimeOnly(10, 0), new TimeOnly(11, 30), CourseFrequency.Weekly);
        var existingCourse = CreateCourse(existingCourseId, DayOfWeek.Monday, new TimeOnly(10, 0), new TimeOnly(11, 30), CourseFrequency.Weekly);

        var existingEnrollment = CreateEnrollment(studentId, existingCourse);

        _mockUnitOfWork.Setup(u => u.Courses.GetByIdAsync(targetCourseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetCourse);

        _mockUnitOfWork.Setup(u => u.Enrollments.GetActiveEnrollmentsByStudentIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Enrollment> { existingEnrollment });

        // Act
        var result = await _service.HasConflictAsync(studentId, targetCourseId);

        // Assert
        Assert.True(result.HasConflict);
        Assert.Single(result.ConflictingCourses);
        Assert.Equal(existingCourseId, result.ConflictingCourses.First().CourseId);
    }

    [Fact]
    public async Task HasConflictAsync_BiweeklyDifferentParity_ShouldReturnNoConflict()
    {
        // KEY TEST: Odd vs Even weeks on same timeslot should NOT conflict

        // Arrange
        var studentId = Guid.NewGuid();
        var targetCourseId = Guid.NewGuid();
        var existingCourseId = Guid.NewGuid();

        var targetCourse = CreateCourse(targetCourseId, DayOfWeek.Monday, new TimeOnly(10, 0), new TimeOnly(11, 30),
            CourseFrequency.Biweekly, WeekParity.Odd);
        var existingCourse = CreateCourse(existingCourseId, DayOfWeek.Monday, new TimeOnly(10, 0), new TimeOnly(11, 30),
            CourseFrequency.Biweekly, WeekParity.Even);

        var existingEnrollment = CreateEnrollment(studentId, existingCourse);

        _mockUnitOfWork.Setup(u => u.Courses.GetByIdAsync(targetCourseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetCourse);

        _mockUnitOfWork.Setup(u => u.Enrollments.GetActiveEnrollmentsByStudentIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Enrollment> { existingEnrollment });

        // Act
        var result = await _service.HasConflictAsync(studentId, targetCourseId);

        // Assert
        Assert.False(result.HasConflict);
        Assert.Empty(result.ConflictingCourses);
    }

    [Fact]
    public async Task HasConflictAsync_BiweeklySameParity_ShouldReturnConflict()
    {
        // KEY TEST: Odd vs Odd weeks on same timeslot SHOULD conflict

        // Arrange
        var studentId = Guid.NewGuid();
        var targetCourseId = Guid.NewGuid();
        var existingCourseId = Guid.NewGuid();

        var targetCourse = CreateCourse(targetCourseId, DayOfWeek.Monday, new TimeOnly(10, 0), new TimeOnly(11, 30),
            CourseFrequency.Biweekly, WeekParity.Odd);
        var existingCourse = CreateCourse(existingCourseId, DayOfWeek.Monday, new TimeOnly(10, 0), new TimeOnly(11, 30),
            CourseFrequency.Biweekly, WeekParity.Odd);

        var existingEnrollment = CreateEnrollment(studentId, existingCourse);

        _mockUnitOfWork.Setup(u => u.Courses.GetByIdAsync(targetCourseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetCourse);

        _mockUnitOfWork.Setup(u => u.Enrollments.GetActiveEnrollmentsByStudentIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Enrollment> { existingEnrollment });

        // Act
        var result = await _service.HasConflictAsync(studentId, targetCourseId);

        // Assert
        Assert.True(result.HasConflict);
        Assert.Single(result.ConflictingCourses);
        Assert.Equal(existingCourseId, result.ConflictingCourses.First().CourseId);
    }

    [Fact]
    public async Task HasConflictAsync_WeeklyVsBiweekly_ShouldReturnConflict()
    {
        // Weekly course conflicts with any biweekly course on same timeslot

        // Arrange
        var studentId = Guid.NewGuid();
        var targetCourseId = Guid.NewGuid();
        var existingCourseId = Guid.NewGuid();

        var targetCourse = CreateCourse(targetCourseId, DayOfWeek.Monday, new TimeOnly(10, 0), new TimeOnly(11, 30),
            CourseFrequency.Weekly, WeekParity.All);
        var existingCourse = CreateCourse(existingCourseId, DayOfWeek.Monday, new TimeOnly(10, 0), new TimeOnly(11, 30),
            CourseFrequency.Biweekly, WeekParity.Odd);

        var existingEnrollment = CreateEnrollment(studentId, existingCourse);

        _mockUnitOfWork.Setup(u => u.Courses.GetByIdAsync(targetCourseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetCourse);

        _mockUnitOfWork.Setup(u => u.Enrollments.GetActiveEnrollmentsByStudentIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Enrollment> { existingEnrollment });

        // Act
        var result = await _service.HasConflictAsync(studentId, targetCourseId);

        // Assert
        Assert.True(result.HasConflict);
        Assert.Single(result.ConflictingCourses);
    }

    [Fact]
    public async Task HasConflictAsync_PartialOverlap_ShouldReturnConflict()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var targetCourseId = Guid.NewGuid();
        var existingCourseId = Guid.NewGuid();

        var targetCourse = CreateCourse(targetCourseId, DayOfWeek.Monday, new TimeOnly(10, 0), new TimeOnly(11, 30));
        var existingCourse = CreateCourse(existingCourseId, DayOfWeek.Monday, new TimeOnly(11, 0), new TimeOnly(12, 0));

        var existingEnrollment = CreateEnrollment(studentId, existingCourse);

        _mockUnitOfWork.Setup(u => u.Courses.GetByIdAsync(targetCourseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetCourse);

        _mockUnitOfWork.Setup(u => u.Enrollments.GetActiveEnrollmentsByStudentIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Enrollment> { existingEnrollment });

        // Act
        var result = await _service.HasConflictAsync(studentId, targetCourseId);

        // Assert
        Assert.True(result.HasConflict);
        Assert.Single(result.ConflictingCourses);
    }

    [Fact]
    public async Task HasConflictAsync_MultipleConflicts_ShouldReturnAll()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var targetCourseId = Guid.NewGuid();
        var conflictingCourse1Id = Guid.NewGuid();
        var conflictingCourse2Id = Guid.NewGuid();
        var nonConflictingCourseId = Guid.NewGuid();

        var targetCourse = CreateCourse(targetCourseId, DayOfWeek.Monday, new TimeOnly(10, 0), new TimeOnly(11, 30));
        var conflictingCourse1 = CreateCourse(conflictingCourse1Id, DayOfWeek.Monday, new TimeOnly(10, 0), new TimeOnly(11, 30));
        var conflictingCourse2 = CreateCourse(conflictingCourse2Id, DayOfWeek.Monday, new TimeOnly(10, 30), new TimeOnly(12, 0));
        var nonConflictingCourse = CreateCourse(nonConflictingCourseId, DayOfWeek.Tuesday, new TimeOnly(10, 0), new TimeOnly(11, 30));

        var enrollments = new List<Enrollment>
        {
            CreateEnrollment(studentId, conflictingCourse1),
            CreateEnrollment(studentId, conflictingCourse2),
            CreateEnrollment(studentId, nonConflictingCourse)
        };

        _mockUnitOfWork.Setup(u => u.Courses.GetByIdAsync(targetCourseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetCourse);

        _mockUnitOfWork.Setup(u => u.Enrollments.GetActiveEnrollmentsByStudentIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(enrollments);

        // Act
        var result = await _service.HasConflictAsync(studentId, targetCourseId);

        // Assert
        Assert.True(result.HasConflict);
        Assert.Equal(2, result.ConflictingCourses.Count());
        Assert.Contains(result.ConflictingCourses, c => c.CourseId == conflictingCourse1Id);
        Assert.Contains(result.ConflictingCourses, c => c.CourseId == conflictingCourse2Id);
    }

    [Fact]
    public async Task HasConflictAsync_BiweeklyAllParityVsOdd_ShouldReturnConflict()
    {
        // Biweekly with All parity means every week, so it conflicts with specific parities

        // Arrange
        var studentId = Guid.NewGuid();
        var targetCourseId = Guid.NewGuid();
        var existingCourseId = Guid.NewGuid();

        var targetCourse = CreateCourse(targetCourseId, DayOfWeek.Monday, new TimeOnly(10, 0), new TimeOnly(11, 30),
            CourseFrequency.Biweekly, WeekParity.All);
        var existingCourse = CreateCourse(existingCourseId, DayOfWeek.Monday, new TimeOnly(10, 0), new TimeOnly(11, 30),
            CourseFrequency.Biweekly, WeekParity.Odd);

        var existingEnrollment = CreateEnrollment(studentId, existingCourse);

        _mockUnitOfWork.Setup(u => u.Courses.GetByIdAsync(targetCourseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetCourse);

        _mockUnitOfWork.Setup(u => u.Enrollments.GetActiveEnrollmentsByStudentIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Enrollment> { existingEnrollment });

        // Act
        var result = await _service.HasConflictAsync(studentId, targetCourseId);

        // Assert
        Assert.True(result.HasConflict);
        Assert.Single(result.ConflictingCourses);
    }

    [Fact]
    public async Task HasConflictAsync_CourseNotFound_ShouldThrowException()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var courseId = Guid.NewGuid();

        _mockUnitOfWork.Setup(u => u.Courses.GetByIdAsync(courseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Course?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.HasConflictAsync(studentId, courseId));
    }

    private static Course CreateCourse(
        Guid id,
        DayOfWeek dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime,
        CourseFrequency frequency = CourseFrequency.Weekly,
        WeekParity weekParity = WeekParity.All)
    {
        return new Course
        {
            Id = id,
            DayOfWeek = dayOfWeek,
            StartTime = startTime,
            EndTime = endTime,
            Frequency = frequency,
            WeekParity = weekParity,
            CourseType = new CourseType { Id = Guid.NewGuid(), Name = "Test Type" },
            Teacher = new Teacher { Id = Guid.NewGuid(), FirstName = "Test", LastName = "Teacher", Email = "test@example.com" }
        };
    }

    private static Enrollment CreateEnrollment(Guid studentId, Course course)
    {
        return new Enrollment
        {
            Id = Guid.NewGuid(),
            StudentId = studentId,
            CourseId = course.Id,
            Course = course,
            Status = EnrollmentStatus.Active,
            EnrolledAt = DateTime.UtcNow
        };
    }
}
