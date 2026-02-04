using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using BosDAT.API.Controllers;
using BosDAT.API.Tests.Helpers;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Core.Interfaces;

namespace BosDAT.API.Tests.Controllers.LessonsController;

/// <summary>
/// Tests for group/workshop lesson generation behavior.
/// Verifies that Group and Workshop courses create one lesson per enrolled student per date,
/// matching the behavior of Individual courses for consistent invoicing.
/// </summary>
public class GroupLessonGenerationTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly BosDAT.API.Controllers.LessonsController _controller;
    private readonly List<Lesson> _createdLessons;

    public GroupLessonGenerationTests()
    {
        _mockUnitOfWork = MockHelpers.CreateMockUnitOfWork();
        _controller = new BosDAT.API.Controllers.LessonsController(_mockUnitOfWork.Object);
        _createdLessons = new List<Lesson>();
    }

    [Fact]
    public async Task GenerateLessons_GroupCourse_FiveStudents_CreatesLessonPerStudentPerDate()
    {
        // Arrange
        var course = CreateTestCourseWithStudents(
            CourseFrequency.Weekly,
            DayOfWeek.Thursday,
            CourseTypeCategory.Group,
            studentCount: 5);
        SetupMocksForCourse(course, new List<Lesson>(), new List<Holiday>());

        var dto = CreateGenerateDto(
            course.Id,
            new DateOnly(2024, 3, 7),   // Thursday
            new DateOnly(2024, 3, 28)); // Thursday (4 weeks)

        // Act
        var result = await _controller.GenerateLessons(dto, CancellationToken.None);

        // Assert
        // 4 weeks × 5 students = 20 lessons
        AssertGenerationResult(result, lessonsCreated: 20, lessonsSkipped: 0);

        // Verify each date has 5 lessons (one per student)
        var lessonsByDate = _createdLessons.GroupBy(l => l.ScheduledDate).ToList();
        Assert.Equal(4, lessonsByDate.Count);
        Assert.All(lessonsByDate, g => Assert.Equal(5, g.Count()));
    }

    [Fact]
    public async Task GenerateLessons_WorkshopCourse_TenStudents_CreatesLessonPerStudentPerDate()
    {
        // Arrange
        var course = CreateTestCourseWithStudents(
            CourseFrequency.Weekly,
            DayOfWeek.Saturday,
            CourseTypeCategory.Workshop,
            studentCount: 10);
        SetupMocksForCourse(course, new List<Lesson>(), new List<Holiday>());

        var dto = CreateGenerateDto(
            course.Id,
            new DateOnly(2024, 3, 2),   // Saturday
            new DateOnly(2024, 3, 30)); // Saturday (5 weeks)

        // Act
        var result = await _controller.GenerateLessons(dto, CancellationToken.None);

        // Assert
        // 5 weeks × 10 students = 50 lessons
        AssertGenerationResult(result, lessonsCreated: 50, lessonsSkipped: 0);

        // Verify each date has 10 lessons (one per student)
        var lessonsByDate = _createdLessons.GroupBy(l => l.ScheduledDate).ToList();
        Assert.Equal(5, lessonsByDate.Count);
        Assert.All(lessonsByDate, g => Assert.Equal(10, g.Count()));
    }

    [Fact]
    public async Task GenerateLessons_GroupCourse_AllLessonsShareSameCourseIdDateTime()
    {
        // Arrange
        var course = CreateTestCourseWithStudents(
            CourseFrequency.Weekly,
            DayOfWeek.Monday,
            CourseTypeCategory.Group,
            studentCount: 3);
        SetupMocksForCourse(course, new List<Lesson>(), new List<Holiday>());

        var dto = CreateGenerateDto(
            course.Id,
            new DateOnly(2024, 3, 4),   // Monday
            new DateOnly(2024, 3, 11)); // Monday (2 weeks)

        // Act
        await _controller.GenerateLessons(dto, CancellationToken.None);

        // Assert - all lessons on same date share CourseId, StartTime, EndTime
        var lessonsOnFirstDate = _createdLessons.Where(l => l.ScheduledDate == new DateOnly(2024, 3, 4)).ToList();
        Assert.Equal(3, lessonsOnFirstDate.Count);
        Assert.All(lessonsOnFirstDate, l =>
        {
            Assert.Equal(course.Id, l.CourseId);
            Assert.Equal(course.StartTime, l.StartTime);
            Assert.Equal(course.EndTime, l.EndTime);
        });
    }

    [Fact]
    public async Task GenerateLessons_GroupCourse_EachStudentGetsUniqueStudentId()
    {
        // Arrange
        var course = CreateTestCourseWithStudents(
            CourseFrequency.Weekly,
            DayOfWeek.Wednesday,
            CourseTypeCategory.Group,
            studentCount: 4);
        var enrolledStudentIds = course.Enrollments.Select(e => e.StudentId).ToHashSet();
        SetupMocksForCourse(course, new List<Lesson>(), new List<Holiday>());

        var dto = CreateGenerateDto(
            course.Id,
            new DateOnly(2024, 3, 6),   // Wednesday
            new DateOnly(2024, 3, 6));  // Single day

        // Act
        await _controller.GenerateLessons(dto, CancellationToken.None);

        // Assert
        Assert.Equal(4, _createdLessons.Count);

        // Each enrolled student should have exactly one lesson
        Assert.All(_createdLessons, l => Assert.NotNull(l.StudentId));

        // Verify the created lessons match enrolled students
        var createdStudentIds = _createdLessons
            .Select(l => l.StudentId!.Value)
            .ToHashSet();
        Assert.Equal(enrolledStudentIds, createdStudentIds);
    }

    [Fact]
    public async Task GenerateLessons_MidEnrollment_OnlyGeneratesFromEnrollmentDateForward()
    {
        // Arrange
        var course = CreateTestCourseWithMidEnrollment();
        SetupMocksForCourse(course, new List<Lesson>(), new List<Holiday>());

        // Course has:
        // - Student1: enrolled from 2024-03-01 (before period)
        // - Student2: enrolled from 2024-03-15 (mid-period)
        // Generation period: 2024-03-04 to 2024-03-25 (4 Mondays)

        var dto = CreateGenerateDto(
            course.Id,
            new DateOnly(2024, 3, 4),   // Monday
            new DateOnly(2024, 3, 25)); // Monday (4 weeks)

        // Act
        await _controller.GenerateLessons(dto, CancellationToken.None);

        // Assert
        // Week 1 (Mar 4): 1 lesson (only Student1)
        // Week 2 (Mar 11): 1 lesson (only Student1)
        // Week 3 (Mar 18): 2 lessons (Student1 + Student2 enrolled Mar 15)
        // Week 4 (Mar 25): 2 lessons (Student1 + Student2)
        // Total: 6 lessons
        var result = _createdLessons;
        Assert.Equal(6, result.Count);

        // Verify Student2 only appears from March 18 onwards
        var student2Id = course.Enrollments.First(e => e.Student.FirstName == "Student2").StudentId;
        var student2Lessons = result.Where(l => l.StudentId == student2Id).ToList();
        Assert.Equal(2, student2Lessons.Count);
        Assert.All(student2Lessons, l => Assert.True(l.ScheduledDate >= new DateOnly(2024, 3, 18)));
    }

    [Fact]
    public async Task GenerateLessons_NoEnrollments_CreatesPlaceholderWithNullStudent()
    {
        // Arrange
        var course = CreateTestCourse(CourseFrequency.Weekly, DayOfWeek.Friday, CourseTypeCategory.Group);
        // No enrollments
        SetupMocksForCourse(course, new List<Lesson>(), new List<Holiday>());

        var dto = CreateGenerateDto(
            course.Id,
            new DateOnly(2024, 3, 1),   // Friday
            new DateOnly(2024, 3, 22)); // Friday (4 weeks)

        // Act
        var result = await _controller.GenerateLessons(dto, CancellationToken.None);

        // Assert
        // 4 weeks, no students = 4 placeholder lessons with null StudentId
        AssertGenerationResult(result, lessonsCreated: 4, lessonsSkipped: 0);
        Assert.Equal(4, _createdLessons.Count);
        Assert.All(_createdLessons, l => Assert.Null(l.StudentId));
    }

    [Fact]
    public async Task GenerateLessons_SkipsDuplicates_ExistingLessonForSameDateAndStudent()
    {
        // Arrange
        var course = CreateTestCourseWithStudents(
            CourseFrequency.Weekly,
            DayOfWeek.Tuesday,
            CourseTypeCategory.Group,
            studentCount: 3);

        var student1Id = course.Enrollments.First().StudentId;
        var existingLessons = new List<Lesson>
        {
            // Student1 already has a lesson on March 5
            new Lesson
            {
                Id = Guid.NewGuid(),
                CourseId = course.Id,
                StudentId = student1Id,
                TeacherId = course.TeacherId,
                ScheduledDate = new DateOnly(2024, 3, 5),
                StartTime = course.StartTime,
                EndTime = course.EndTime,
                Status = LessonStatus.Scheduled
            }
        };
        SetupMocksForCourse(course, existingLessons, new List<Holiday>());

        var dto = CreateGenerateDto(
            course.Id,
            new DateOnly(2024, 3, 5),   // Tuesday
            new DateOnly(2024, 3, 5));  // Single day

        // Act
        var result = await _controller.GenerateLessons(dto, CancellationToken.None);

        // Assert
        // Only 2 new lessons (students 2 and 3), student 1 skipped
        AssertGenerationResult(result, lessonsCreated: 2, lessonsSkipped: 1);

        // Verify the created lessons are for students 2 and 3 only
        Assert.DoesNotContain(_createdLessons, l => l.StudentId == student1Id);
    }

    #region Helper Methods

    private static GenerateLessonsDto CreateGenerateDto(
        Guid courseId,
        DateOnly startDate,
        DateOnly endDate,
        bool skipHolidays = true)
    {
        return new GenerateLessonsDto
        {
            CourseId = courseId,
            StartDate = startDate,
            EndDate = endDate,
            SkipHolidays = skipHolidays
        };
    }

    private static void AssertGenerationResult(
        ActionResult<GenerateLessonsResultDto> result,
        int lessonsCreated,
        int lessonsSkipped)
    {
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var generateResult = Assert.IsType<GenerateLessonsResultDto>(okResult.Value);

        Assert.Equal(lessonsCreated, generateResult.LessonsCreated);
        Assert.Equal(lessonsSkipped, generateResult.LessonsSkipped);
    }

    private static Course CreateTestCourse(CourseFrequency frequency, DayOfWeek dayOfWeek, CourseTypeCategory category)
    {
        var courseId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var instrument = CreateTestInstrument();
        var courseType = CreateTestCourseType(instrument, category);

        return new Course
        {
            Id = courseId,
            TeacherId = teacherId,
            CourseTypeId = courseType.Id,
            RoomId = 1,
            DayOfWeek = dayOfWeek,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(10, 30),
            Frequency = frequency,
            WeekParity = WeekParity.All,
            Status = CourseStatus.Active,
            CourseType = courseType,
            Enrollments = new List<Enrollment>()
        };
    }

    private static Course CreateTestCourseWithStudents(
        CourseFrequency frequency,
        DayOfWeek dayOfWeek,
        CourseTypeCategory category,
        int studentCount)
    {
        var courseId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var instrument = CreateTestInstrument();
        var courseType = CreateTestCourseType(instrument, category);

        var enrollments = new List<Enrollment>();
        for (var i = 0; i < studentCount; i++)
        {
            var studentId = Guid.NewGuid();
            enrollments.Add(new Enrollment
            {
                Id = Guid.NewGuid(),
                StudentId = studentId,
                CourseId = courseId,
                Status = EnrollmentStatus.Active,
                EnrolledAt = new DateTime(2024, 1, 1), // All enrolled before test period
                Student = new Student
                {
                    Id = studentId,
                    FirstName = $"Student{i + 1}",
                    LastName = "Test",
                    Email = $"student{i + 1}@test.com"
                }
            });
        }

        return new Course
        {
            Id = courseId,
            TeacherId = teacherId,
            CourseTypeId = courseType.Id,
            RoomId = 1,
            DayOfWeek = dayOfWeek,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(10, 30),
            Frequency = frequency,
            WeekParity = WeekParity.All,
            Status = CourseStatus.Active,
            CourseType = courseType,
            Enrollments = enrollments
        };
    }

    private static Course CreateTestCourseWithMidEnrollment()
    {
        var courseId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var instrument = CreateTestInstrument();
        var courseType = CreateTestCourseType(instrument, CourseTypeCategory.Group);

        var student1Id = Guid.NewGuid();
        var student2Id = Guid.NewGuid();

        var enrollments = new List<Enrollment>
        {
            new Enrollment
            {
                Id = Guid.NewGuid(),
                StudentId = student1Id,
                CourseId = courseId,
                Status = EnrollmentStatus.Active,
                EnrolledAt = new DateTime(2024, 3, 1), // Before period starts
                Student = new Student
                {
                    Id = student1Id,
                    FirstName = "Student1",
                    LastName = "Test",
                    Email = "student1@test.com"
                }
            },
            new Enrollment
            {
                Id = Guid.NewGuid(),
                StudentId = student2Id,
                CourseId = courseId,
                Status = EnrollmentStatus.Active,
                EnrolledAt = new DateTime(2024, 3, 15), // Mid-period enrollment
                Student = new Student
                {
                    Id = student2Id,
                    FirstName = "Student2",
                    LastName = "Test",
                    Email = "student2@test.com"
                }
            }
        };

        return new Course
        {
            Id = courseId,
            TeacherId = teacherId,
            CourseTypeId = courseType.Id,
            RoomId = 1,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(10, 30),
            Frequency = CourseFrequency.Weekly,
            WeekParity = WeekParity.All,
            Status = CourseStatus.Active,
            CourseType = courseType,
            Enrollments = enrollments
        };
    }

    private static Instrument CreateTestInstrument()
    {
        return new Instrument
        {
            Id = 1,
            Name = "Piano",
            Category = InstrumentCategory.Keyboard
        };
    }

    private static CourseType CreateTestCourseType(Instrument instrument, CourseTypeCategory category)
    {
        return new CourseType
        {
            Id = Guid.NewGuid(),
            Name = $"Test {category} Course",
            InstrumentId = instrument.Id,
            Instrument = instrument,
            Type = category,
            DurationMinutes = 30,
            MaxStudents = category == CourseTypeCategory.Individual ? 1 : 10
        };
    }

    private void SetupMocksForCourse(Course course, List<Lesson> existingLessons, List<Holiday> holidays)
    {
        var mockCourseRepo = new Mock<ICourseRepository>();
        mockCourseRepo.Setup(r => r.Query())
            .Returns(new List<Course> { course }.AsQueryable().BuildMockDbSet().Object);

        var mockLessonRepo = new Mock<ILessonRepository>();
        mockLessonRepo.Setup(r => r.Query())
            .Returns(existingLessons.AsQueryable().BuildMockDbSet().Object);
        mockLessonRepo.Setup(r => r.AddAsync(It.IsAny<Lesson>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lesson l, CancellationToken _) =>
            {
                _createdLessons.Add(l);
                return l;
            });

        var mockHolidayRepo = MockHelpers.CreateMockRepository(holidays);

        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);
        _mockUnitOfWork.Setup(u => u.Lessons).Returns(mockLessonRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<Holiday>()).Returns(mockHolidayRepo.Object);
    }

    #endregion
}
