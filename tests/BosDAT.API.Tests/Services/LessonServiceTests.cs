using Moq;
using Xunit;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Core.Interfaces;
using BosDAT.Core.Interfaces.Services;
using BosDAT.Core.Interfaces.Repositories;
using BosDAT.Infrastructure.Services;
using BosDAT.API.Tests.Helpers;
using static BosDAT.API.Tests.Helpers.TestDataFactory;

namespace BosDAT.API.Tests.Services;

public class LessonServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILessonRepository> _mockLessonRepo;
    private readonly LessonService _service;

    public LessonServiceTests()
    {
        _mockUnitOfWork = MockHelpers.CreateMockUnitOfWork();
        _mockLessonRepo = new Mock<ILessonRepository>();
        _mockUnitOfWork.Setup(u => u.Lessons).Returns(_mockLessonRepo.Object);
        _service = new LessonService(_mockUnitOfWork.Object);
    }

    private static Lesson CreateLesson(Teacher teacher, Course course, Student? student = null, bool isInvoiced = false)
    {
        return new Lesson
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            Course = course,
            TeacherId = teacher.Id,
            Teacher = teacher,
            StudentId = student?.Id,
            Student = student,
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 0),
            Status = LessonStatus.Scheduled,
            IsInvoiced = isInvoiced
        };
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsLessonDto()
    {
        var teacher = CreateTeacher();
        var instrument = CreateInstrument();
        var courseType = CreateCourseType(instrument);
        var course = CreateCourse(teacher, courseType);
        var lesson = CreateLesson(teacher, course);
        var lessons = new List<Lesson> { lesson };

        _mockLessonRepo.Setup(r => r.Query())
            .Returns(() => lessons.AsQueryable().BuildMockDbSet().Object);

        var result = await _service.GetByIdAsync(lesson.Id);

        Assert.NotNull(result);
        Assert.Equal(lesson.Id, result.Id);
        Assert.Equal(teacher.FullName, result.TeacherName);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
    {
        _mockLessonRepo.Setup(r => r.Query())
            .Returns(new List<Lesson>().AsQueryable().BuildMockDbSet().Object);

        var result = await _service.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    #endregion

    #region GetByStudentAsync Tests

    [Fact]
    public async Task GetByStudentAsync_WithValidStudent_ReturnsLessons()
    {
        var teacher = CreateTeacher();
        var instrument = CreateInstrument();
        var courseType = CreateCourseType(instrument);
        var course = CreateCourse(teacher, courseType);
        var student = CreateStudent();
        var lesson = CreateLesson(teacher, course, student);

        _mockLessonRepo
            .Setup(r => r.GetByStudentAsync(student.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Lesson> { lesson });

        var result = await _service.GetByStudentAsync(student.Id);

        Assert.Single(result);
        Assert.Equal(lesson.Id, result[0].Id);
        Assert.Equal(student.Id, result[0].StudentId);
    }

    [Fact]
    public async Task GetByStudentAsync_WithNoLessons_ReturnsEmptyList()
    {
        _mockLessonRepo
            .Setup(r => r.GetByStudentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Lesson>());

        var result = await _service.GetByStudentAsync(Guid.NewGuid());

        Assert.Empty(result);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithInvalidCourse_ReturnsError()
    {
        var mockCourseRepo = MockHelpers.CreateMockCourseRepository(new List<Course>());
        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);

        var dto = new CreateLessonDto
        {
            CourseId = Guid.NewGuid(),
            TeacherId = Guid.NewGuid(),
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 0)
        };

        var (lesson, error) = await _service.CreateAsync(dto);

        Assert.Null(lesson);
        Assert.Equal("Course not found", error);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidTeacher_ReturnsError()
    {
        var teacher = CreateTeacher();
        var instrument = CreateInstrument();
        var courseType = CreateCourseType(instrument);
        var course = CreateCourse(teacher, courseType);

        var mockCourseRepo = MockHelpers.CreateMockCourseRepository(new List<Course> { course });
        var mockTeacherRepo = MockHelpers.CreateMockTeacherRepository(new List<Teacher>());
        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);
        _mockUnitOfWork.Setup(u => u.Teachers).Returns(mockTeacherRepo.Object);

        var dto = new CreateLessonDto
        {
            CourseId = course.Id,
            TeacherId = Guid.NewGuid(),
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 0)
        };

        var (lesson, error) = await _service.CreateAsync(dto);

        Assert.Null(lesson);
        Assert.Equal("Teacher not found", error);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidStudent_ReturnsError()
    {
        var teacher = CreateTeacher();
        var instrument = CreateInstrument();
        var courseType = CreateCourseType(instrument);
        var course = CreateCourse(teacher, courseType);

        var mockCourseRepo = MockHelpers.CreateMockCourseRepository(new List<Course> { course });
        var mockTeacherRepo = MockHelpers.CreateMockTeacherRepository(new List<Teacher> { teacher });
        var mockStudentRepo = MockHelpers.CreateMockStudentRepository(new List<Student>());
        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);
        _mockUnitOfWork.Setup(u => u.Teachers).Returns(mockTeacherRepo.Object);
        _mockUnitOfWork.Setup(u => u.Students).Returns(mockStudentRepo.Object);

        var dto = new CreateLessonDto
        {
            CourseId = course.Id,
            TeacherId = teacher.Id,
            StudentId = Guid.NewGuid(),
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 0)
        };

        var (lesson, error) = await _service.CreateAsync(dto);

        Assert.Null(lesson);
        Assert.Equal("Student not found", error);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithInvalidId_ReturnsNotFound()
    {
        _mockLessonRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lesson?)null);

        var dto = new UpdateLessonDto
        {
            TeacherId = Guid.NewGuid(),
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 0),
            Status = LessonStatus.Scheduled
        };

        var (lesson, notFound) = await _service.UpdateAsync(Guid.NewGuid(), dto);

        Assert.Null(lesson);
        Assert.True(notFound);
    }

    [Fact]
    public async Task UpdateAsync_WithValidId_UpdatesFieldsAndReturnsDto()
    {
        var teacher = CreateTeacher();
        var instrument = CreateInstrument();
        var courseType = CreateCourseType(instrument);
        var course = CreateCourse(teacher, courseType);
        var lesson = CreateLesson(teacher, course);
        var lessons = new List<Lesson> { lesson };

        _mockLessonRepo
            .Setup(r => r.GetByIdAsync(lesson.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);
        _mockLessonRepo.Setup(r => r.Query())
            .Returns(() => lessons.AsQueryable().BuildMockDbSet().Object);

        var newDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        var dto = new UpdateLessonDto
        {
            TeacherId = teacher.Id,
            ScheduledDate = newDate,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0),
            Status = LessonStatus.Completed
        };

        var (result, notFound) = await _service.UpdateAsync(lesson.Id, dto);

        Assert.False(notFound);
        Assert.NotNull(result);
        Assert.Equal(newDate, lesson.ScheduledDate);
        Assert.Equal(LessonStatus.Completed, lesson.Status);
    }

    #endregion

    #region UpdateStatusAsync Tests

    [Fact]
    public async Task UpdateStatusAsync_WithInvalidId_ReturnsNotFound()
    {
        _mockLessonRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lesson?)null);

        var (lesson, notFound) = await _service.UpdateStatusAsync(Guid.NewGuid(), LessonStatus.Cancelled, "reason");

        Assert.Null(lesson);
        Assert.True(notFound);
    }

    [Fact]
    public async Task UpdateStatusAsync_WithValidId_UpdatesStatusAndReason()
    {
        var teacher = CreateTeacher();
        var instrument = CreateInstrument();
        var courseType = CreateCourseType(instrument);
        var course = CreateCourse(teacher, courseType);
        var lesson = CreateLesson(teacher, course);
        var lessons = new List<Lesson> { lesson };

        _mockLessonRepo
            .Setup(r => r.GetByIdAsync(lesson.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);
        _mockLessonRepo.Setup(r => r.Query())
            .Returns(() => lessons.AsQueryable().BuildMockDbSet().Object);

        var (result, notFound) = await _service.UpdateStatusAsync(lesson.Id, LessonStatus.Cancelled, "Sick");

        Assert.False(notFound);
        Assert.NotNull(result);
        Assert.Equal(LessonStatus.Cancelled, lesson.Status);
        Assert.Equal("Sick", lesson.CancellationReason);
    }

    #endregion

    #region UpdateGroupStatusAsync Tests

    [Fact]
    public async Task UpdateGroupStatusAsync_WithNoMatchingLessons_ReturnsNotFound()
    {
        _mockLessonRepo.Setup(r => r.Query())
            .Returns(new List<Lesson>().AsQueryable().BuildMockDbSet().Object);

        var (count, notFound) = await _service.UpdateGroupStatusAsync(
            Guid.NewGuid(), DateOnly.FromDateTime(DateTime.Today), LessonStatus.Cancelled, null);

        Assert.Equal(0, count);
        Assert.True(notFound);
    }

    [Fact]
    public async Task UpdateGroupStatusAsync_WithMatchingLessons_UpdatesAllAndReturnsCount()
    {
        var teacher = CreateTeacher();
        var instrument = CreateInstrument();
        var courseType = CreateCourseType(instrument);
        var course = CreateCourse(teacher, courseType);
        var scheduledDate = DateOnly.FromDateTime(DateTime.Today);
        var lesson1 = CreateLesson(teacher, course);
        lesson1.ScheduledDate = scheduledDate;
        var lesson2 = CreateLesson(teacher, course);
        lesson2.ScheduledDate = scheduledDate;
        var lessons = new List<Lesson> { lesson1, lesson2 };

        _mockLessonRepo.Setup(r => r.Query())
            .Returns(() => lessons.AsQueryable().BuildMockDbSet().Object);

        var (count, notFound) = await _service.UpdateGroupStatusAsync(
            course.Id, scheduledDate, LessonStatus.Cancelled, "Weather");

        Assert.Equal(2, count);
        Assert.False(notFound);
        Assert.All(lessons, l => Assert.Equal(LessonStatus.Cancelled, l.Status));
        Assert.All(lessons, l => Assert.Equal("Weather", l.CancellationReason));
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithInvalidId_ReturnsError()
    {
        _mockLessonRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lesson?)null);

        var (success, error) = await _service.DeleteAsync(Guid.NewGuid());

        Assert.False(success);
        Assert.Equal("Lesson not found", error);
    }

    [Fact]
    public async Task DeleteAsync_WithInvoicedLesson_ReturnsError()
    {
        var teacher = CreateTeacher();
        var instrument = CreateInstrument();
        var courseType = CreateCourseType(instrument);
        var course = CreateCourse(teacher, courseType);
        var lesson = CreateLesson(teacher, course, isInvoiced: true);

        _mockLessonRepo
            .Setup(r => r.GetByIdAsync(lesson.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);

        var (success, error) = await _service.DeleteAsync(lesson.Id);

        Assert.False(success);
        Assert.Contains("invoiced", error);
    }

    [Fact]
    public async Task DeleteAsync_WithValidUninvoicedLesson_DeletesSuccessfully()
    {
        var teacher = CreateTeacher();
        var instrument = CreateInstrument();
        var courseType = CreateCourseType(instrument);
        var course = CreateCourse(teacher, courseType);
        var lesson = CreateLesson(teacher, course, isInvoiced: false);

        _mockLessonRepo
            .Setup(r => r.GetByIdAsync(lesson.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);
        _mockLessonRepo
            .Setup(r => r.DeleteAsync(It.IsAny<Lesson>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var (success, error) = await _service.DeleteAsync(lesson.Id);

        Assert.True(success);
        Assert.Null(error);
        _mockLessonRepo.Verify(r => r.DeleteAsync(lesson, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
