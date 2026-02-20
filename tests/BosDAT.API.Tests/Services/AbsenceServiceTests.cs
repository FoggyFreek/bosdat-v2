using Moq;
using Xunit;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Core.Interfaces;
using BosDAT.Infrastructure.Services;
using BosDAT.API.Tests.Helpers;

namespace BosDAT.API.Tests.Services;

public class AbsenceServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IRepository<Absence>> _mockAbsenceRepo;
    private readonly Mock<ILessonRepository> _mockLessonRepo;
    private readonly AbsenceService _service;

    public AbsenceServiceTests()
    {
        _mockUnitOfWork = MockHelpers.CreateMockUnitOfWork();
        _mockAbsenceRepo = new Mock<IRepository<Absence>>();
        _mockLessonRepo = new Mock<ILessonRepository>();

        _mockUnitOfWork.Setup(u => u.Repository<Absence>()).Returns(_mockAbsenceRepo.Object);
        _mockUnitOfWork.Setup(u => u.Lessons).Returns(_mockLessonRepo.Object);

        _service = new AbsenceService(_mockUnitOfWork.Object);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllAbsences()
    {
        // Arrange
        var student = new Student { Id = Guid.NewGuid(), FirstName = "John", LastName = "Doe", Email = "john@test.com" };
        var absences = new List<Absence>
        {
            new Absence
            {
                Id = Guid.NewGuid(),
                StudentId = student.Id,
                Student = student,
                StartDate = new DateOnly(2026, 1, 1),
                EndDate = new DateOnly(2026, 1, 7),
                Reason = AbsenceReason.Holiday,
                InvoiceLesson = false
            }
        };

        _mockAbsenceRepo.Setup(r => r.Query())
            .Returns(absences.AsQueryable().BuildMockDbSet().Object);

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        var list = result.ToList();
        Assert.Single(list);
        Assert.Equal("John Doe", list[0].PersonName);
        Assert.Equal(AbsenceReason.Holiday, list[0].Reason);
    }

    [Fact]
    public async Task GetByStudentAsync_ReturnsStudentAbsencesOnly()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var otherStudentId = Guid.NewGuid();
        var student = new Student { Id = studentId, FirstName = "Jane", LastName = "Doe", Email = "jane@test.com" };
        var absences = new List<Absence>
        {
            new Absence
            {
                Id = Guid.NewGuid(),
                StudentId = studentId,
                Student = student,
                StartDate = new DateOnly(2026, 2, 1),
                EndDate = new DateOnly(2026, 2, 3),
                Reason = AbsenceReason.Sick
            },
            new Absence
            {
                Id = Guid.NewGuid(),
                StudentId = otherStudentId,
                StartDate = new DateOnly(2026, 2, 1),
                EndDate = new DateOnly(2026, 2, 3),
                Reason = AbsenceReason.Other
            }
        };

        _mockAbsenceRepo.Setup(r => r.Query())
            .Returns(absences.AsQueryable().BuildMockDbSet().Object);

        // Act
        var result = await _service.GetByStudentAsync(studentId);

        // Assert
        var list = result.ToList();
        Assert.Single(list);
        Assert.Equal(studentId, list[0].StudentId);
    }

    [Fact]
    public async Task GetByTeacherAsync_ReturnsTeacherAbsencesOnly()
    {
        // Arrange
        var teacherId = Guid.NewGuid();
        var teacher = new Teacher { Id = teacherId, FirstName = "Tom", LastName = "Teacher", Email = "tom@test.com" };
        var absences = new List<Absence>
        {
            new Absence
            {
                Id = Guid.NewGuid(),
                TeacherId = teacherId,
                Teacher = teacher,
                StartDate = new DateOnly(2026, 3, 1),
                EndDate = new DateOnly(2026, 3, 5),
                Reason = AbsenceReason.Sick
            }
        };

        _mockAbsenceRepo.Setup(r => r.Query())
            .Returns(absences.AsQueryable().BuildMockDbSet().Object);

        // Act
        var result = await _service.GetByTeacherAsync(teacherId);

        // Assert
        var list = result.ToList();
        Assert.Single(list);
        Assert.Equal(teacherId, list[0].TeacherId);
        Assert.Equal("Tom Teacher", list[0].PersonName);
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsAbsence()
    {
        // Arrange
        var absenceId = Guid.NewGuid();
        var student = new Student { Id = Guid.NewGuid(), FirstName = "Alice", LastName = "Smith", Email = "alice@test.com" };
        var absences = new List<Absence>
        {
            new Absence
            {
                Id = absenceId,
                StudentId = student.Id,
                Student = student,
                StartDate = new DateOnly(2026, 4, 1),
                EndDate = new DateOnly(2026, 4, 5),
                Reason = AbsenceReason.Holiday,
                Notes = "Family vacation"
            }
        };

        _mockAbsenceRepo.Setup(r => r.Query())
            .Returns(absences.AsQueryable().BuildMockDbSet().Object);

        // Act
        var result = await _service.GetByIdAsync(absenceId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(absenceId, result.Id);
        Assert.Equal("Family vacation", result.Notes);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
    {
        // Arrange
        _mockAbsenceRepo.Setup(r => r.Query())
            .Returns(new List<Absence>().AsQueryable().BuildMockDbSet().Object);

        // Act
        var result = await _service.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_WhenExists_DeletesAndReturnsTrue()
    {
        // Arrange
        var absenceId = Guid.NewGuid();
        var absence = new Absence { Id = absenceId, StartDate = new DateOnly(2026, 5, 1), EndDate = new DateOnly(2026, 5, 3), Reason = AbsenceReason.Other };

        _mockAbsenceRepo.Setup(r => r.GetByIdAsync(absenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(absence);

        // Act
        var result = await _service.DeleteAsync(absenceId);

        // Assert
        Assert.True(result);
        _mockAbsenceRepo.Verify(r => r.DeleteAsync(absence, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenNotFound_ReturnsFalse()
    {
        // Arrange
        _mockAbsenceRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Absence?)null);

        // Act
        var result = await _service.DeleteAsync(Guid.NewGuid());

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsTeacherAbsentAsync_WhenAbsent_ReturnsTrue()
    {
        // Arrange
        var teacherId = Guid.NewGuid();

        _mockAbsenceRepo.Setup(r => r.AnyAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Absence, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.IsTeacherAbsentAsync(teacherId, new DateOnly(2026, 3, 3));

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsStudentAbsentAsync_WhenNotAbsent_ReturnsFalse()
    {
        // Arrange
        var studentId = Guid.NewGuid();

        _mockAbsenceRepo.Setup(r => r.AnyAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Absence, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.IsStudentAbsentAsync(studentId, new DateOnly(2026, 3, 3));

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CreateAsync_CancelsAffectedScheduledLessons()
    {
        // Arrange
        var teacherId = Guid.NewGuid();
        var lessonId = Guid.NewGuid();
        var lesson = new Lesson
        {
            Id = lessonId,
            TeacherId = teacherId,
            ScheduledDate = new DateOnly(2026, 6, 3),
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(10, 30),
            Status = LessonStatus.Scheduled
        };

        var absences = new List<Absence>();
        _mockAbsenceRepo.Setup(r => r.AddAsync(It.IsAny<Absence>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Absence entity, CancellationToken _) =>
            {
                absences.Add(entity);
                return entity;
            });

        // For reload query after create
        _mockAbsenceRepo.Setup(r => r.Query())
            .Returns(() => absences.AsQueryable().BuildMockDbSet().Object);

        _mockLessonRepo.Setup(r => r.Query())
            .Returns(new List<Lesson> { lesson }.AsQueryable().BuildMockDbSet().Object);

        var dto = new CreateAbsenceDto
        {
            TeacherId = teacherId,
            StartDate = new DateOnly(2026, 6, 1),
            EndDate = new DateOnly(2026, 6, 7),
            Reason = AbsenceReason.Sick,
            InvoiceLesson = false
        };

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        Assert.Equal(LessonStatus.Cancelled, lesson.Status);
        Assert.Contains("Absence: Sick", lesson.CancellationReason);
        Assert.False(lesson.IsInvoiced);
    }

    [Fact]
    public async Task CreateAsync_WithInvoiceLesson_MarksLessonsAsInvoiced()
    {
        // Arrange
        var teacherId = Guid.NewGuid();
        var lesson = new Lesson
        {
            Id = Guid.NewGuid(),
            TeacherId = teacherId,
            ScheduledDate = new DateOnly(2026, 7, 3),
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(10, 30),
            Status = LessonStatus.Scheduled
        };

        var absences = new List<Absence>();
        _mockAbsenceRepo.Setup(r => r.AddAsync(It.IsAny<Absence>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Absence entity, CancellationToken _) =>
            {
                absences.Add(entity);
                return entity;
            });

        _mockAbsenceRepo.Setup(r => r.Query())
            .Returns(() => absences.AsQueryable().BuildMockDbSet().Object);

        _mockLessonRepo.Setup(r => r.Query())
            .Returns(new List<Lesson> { lesson }.AsQueryable().BuildMockDbSet().Object);

        var dto = new CreateAbsenceDto
        {
            TeacherId = teacherId,
            StartDate = new DateOnly(2026, 7, 1),
            EndDate = new DateOnly(2026, 7, 7),
            Reason = AbsenceReason.Holiday,
            InvoiceLesson = true
        };

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        Assert.Equal(LessonStatus.Cancelled, lesson.Status);
        Assert.True(lesson.IsInvoiced);
    }

    [Theory]
    [InlineData(AbsenceReason.Holiday)]
    [InlineData(AbsenceReason.Sick)]
    [InlineData(AbsenceReason.Other)]
    public async Task GetTeacherAbsencesForPeriodAsync_ReturnsAbsencesInRange(AbsenceReason reason)
    {
        // Arrange
        var teacherId = Guid.NewGuid();
        var teacher = new Teacher { Id = teacherId, FirstName = "Test", LastName = "Teacher", Email = "test@test.com" };
        var absences = new List<Absence>
        {
            new Absence
            {
                Id = Guid.NewGuid(),
                TeacherId = teacherId,
                Teacher = teacher,
                StartDate = new DateOnly(2026, 8, 1),
                EndDate = new DateOnly(2026, 8, 5),
                Reason = reason
            }
        };

        _mockAbsenceRepo.Setup(r => r.Query())
            .Returns(absences.AsQueryable().BuildMockDbSet().Object);

        // Act
        var result = await _service.GetTeacherAbsencesForPeriodAsync(
            new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31));

        // Assert
        var list = result.ToList();
        Assert.Single(list);
        Assert.Equal(reason, list[0].Reason);
    }
}
