using Moq;
using Xunit;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;
using BosDAT.Infrastructure.Services;
using BosDAT.API.Tests.Helpers;

namespace BosDAT.API.Tests.Services;

public class CourseServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly CourseService _service;

    public CourseServiceTests()
    {
        _mockUnitOfWork = MockHelpers.CreateMockUnitOfWork();
        _service = new CourseService(_mockUnitOfWork.Object);
    }

    private static Instrument CreateInstrument(int id = 1, string name = "Piano")
    {
        return new Instrument
        {
            Id = id,
            Name = name,
            Category = InstrumentCategory.Keyboard,
            IsActive = true
        };
    }

    private static CourseType CreateCourseType(Instrument instrument, string name = "Beginner Piano")
    {
        return new CourseType
        {
            Id = Guid.NewGuid(),
            Name = name,
            InstrumentId = instrument.Id,
            Instrument = instrument,
            IsActive = true,
            Type = CourseTypeCategory.Individual,
            MaxStudents = 1,
            DurationMinutes = 30
        };
    }

    private static Teacher CreateTeacher(string firstName = "John", string lastName = "Doe")
    {
        return new Teacher
        {
            Id = Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            Email = $"{firstName.ToLower()}.{lastName.ToLower()}@example.com",
            HourlyRate = 50m,
            IsActive = true,
            Role = TeacherRole.Teacher
        };
    }

    private static Student CreateStudent(string firstName = "Jane", string lastName = "Smith")
    {
        return new Student
        {
            Id = Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            Email = $"{firstName.ToLower()}.{lastName.ToLower()}@example.com",
            Status = StudentStatus.Active
        };
    }

    private static Room CreateRoom(int id = 1, string name = "Room 1")
    {
        return new Room
        {
            Id = id,
            Name = name,
            Capacity = 2,
            IsActive = true
        };
    }

    private static Course CreateCourse(
        Teacher teacher,
        CourseType courseType,
        CourseStatus status = CourseStatus.Active,
        DayOfWeek dayOfWeek = DayOfWeek.Monday,
        Room? room = null)
    {
        return new Course
        {
            Id = Guid.NewGuid(),
            TeacherId = teacher.Id,
            Teacher = teacher,
            CourseTypeId = courseType.Id,
            CourseType = courseType,
            RoomId = room?.Id,
            Room = room,
            DayOfWeek = dayOfWeek,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 0),
            Frequency = CourseFrequency.Weekly,
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            Status = status,
            Enrollments = new List<Enrollment>()
        };
    }

    #region GetSummaryAsync Tests

    [Fact]
    public async Task GetSummaryAsync_WithNoFilters_ReturnsAllCourses()
    {
        // Arrange
        var instrument = CreateInstrument();
        var courseType = CreateCourseType(instrument);
        var teacher = CreateTeacher();
        var courses = new List<Course>
        {
            CreateCourse(teacher, courseType),
            CreateCourse(teacher, courseType, dayOfWeek: DayOfWeek.Wednesday)
        };

        var mockCourseRepo = MockHelpers.CreateMockCourseRepository(courses);
        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);

        // Act
        var result = await _service.GetSummaryAsync(null, null, null, null);

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetSummaryAsync_WithStatusFilter_ReturnsMatchingCourses()
    {
        // Arrange
        var instrument = CreateInstrument();
        var courseType = CreateCourseType(instrument);
        var teacher = CreateTeacher();
        var courses = new List<Course>
        {
            CreateCourse(teacher, courseType, CourseStatus.Active),
            CreateCourse(teacher, courseType, CourseStatus.Cancelled)
        };

        var mockCourseRepo = MockHelpers.CreateMockCourseRepository(courses);
        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);

        // Act
        var result = await _service.GetSummaryAsync(CourseStatus.Active, null, null, null);

        // Assert
        Assert.Single(result);
        Assert.Equal(CourseStatus.Active, result[0].Status);
    }

    [Fact]
    public async Task GetSummaryAsync_WithTeacherFilter_ReturnsMatchingCourses()
    {
        // Arrange
        var instrument = CreateInstrument();
        var courseType = CreateCourseType(instrument);
        var teacher1 = CreateTeacher("John", "Doe");
        var teacher2 = CreateTeacher("Jane", "Smith");
        var courses = new List<Course>
        {
            CreateCourse(teacher1, courseType),
            CreateCourse(teacher2, courseType)
        };

        var mockCourseRepo = MockHelpers.CreateMockCourseRepository(courses);
        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);

        // Act
        var result = await _service.GetSummaryAsync(null, teacher1.Id, null, null);

        // Assert
        Assert.Single(result);
    }

    [Fact]
    public async Task GetSummaryAsync_WithDayOfWeekFilter_ReturnsMatchingCourses()
    {
        // Arrange
        var instrument = CreateInstrument();
        var courseType = CreateCourseType(instrument);
        var teacher = CreateTeacher();
        var courses = new List<Course>
        {
            CreateCourse(teacher, courseType, dayOfWeek: DayOfWeek.Monday),
            CreateCourse(teacher, courseType, dayOfWeek: DayOfWeek.Wednesday)
        };

        var mockCourseRepo = MockHelpers.CreateMockCourseRepository(courses);
        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);

        // Act
        var result = await _service.GetSummaryAsync(null, null, DayOfWeek.Monday, null);

        // Assert
        Assert.Single(result);
        Assert.Equal(DayOfWeek.Monday, result[0].DayOfWeek);
    }

    [Fact]
    public async Task GetSummaryAsync_WithRoomFilter_ReturnsMatchingCourses()
    {
        // Arrange
        var instrument = CreateInstrument();
        var courseType = CreateCourseType(instrument);
        var teacher = CreateTeacher();
        var room1 = CreateRoom(1, "Room 1");
        var room2 = CreateRoom(2, "Room 2");
        var courses = new List<Course>
        {
            CreateCourse(teacher, courseType, room: room1),
            CreateCourse(teacher, courseType, room: room2)
        };

        var mockCourseRepo = MockHelpers.CreateMockCourseRepository(courses);
        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);

        // Act
        var result = await _service.GetSummaryAsync(null, null, null, room1.Id);

        // Assert
        Assert.Single(result);
        Assert.Equal("Room 1", result[0].RoomName);
    }

    [Fact]
    public async Task GetSummaryAsync_WithMultipleFilters_ReturnsMatchingCourses()
    {
        // Arrange
        var instrument = CreateInstrument();
        var courseType = CreateCourseType(instrument);
        var teacher = CreateTeacher();
        var room = CreateRoom();
        var courses = new List<Course>
        {
            CreateCourse(teacher, courseType, CourseStatus.Active, DayOfWeek.Monday, room),
            CreateCourse(teacher, courseType, CourseStatus.Cancelled, DayOfWeek.Monday, room),
            CreateCourse(teacher, courseType, CourseStatus.Active, DayOfWeek.Wednesday, room)
        };

        var mockCourseRepo = MockHelpers.CreateMockCourseRepository(courses);
        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);

        // Act
        var result = await _service.GetSummaryAsync(CourseStatus.Active, teacher.Id, DayOfWeek.Monday, room.Id);

        // Assert
        Assert.Single(result);
    }

    #endregion

    #region GetCountAsync Tests

    [Fact]
    public async Task GetCountAsync_WithNoFilters_ReturnsTotalCount()
    {
        // Arrange
        var instrument = CreateInstrument();
        var courseType = CreateCourseType(instrument);
        var teacher = CreateTeacher();
        var courses = new List<Course>
        {
            CreateCourse(teacher, courseType),
            CreateCourse(teacher, courseType, dayOfWeek: DayOfWeek.Wednesday),
            CreateCourse(teacher, courseType, dayOfWeek: DayOfWeek.Friday)
        };

        var mockCourseRepo = MockHelpers.CreateMockCourseRepository(courses);
        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);

        // Act
        var result = await _service.GetCountAsync(null, null, null, null);

        // Assert
        Assert.Equal(3, result);
    }

    [Fact]
    public async Task GetCountAsync_WithStatusFilter_ReturnsFilteredCount()
    {
        // Arrange
        var instrument = CreateInstrument();
        var courseType = CreateCourseType(instrument);
        var teacher = CreateTeacher();
        var courses = new List<Course>
        {
            CreateCourse(teacher, courseType, CourseStatus.Active),
            CreateCourse(teacher, courseType, CourseStatus.Active),
            CreateCourse(teacher, courseType, CourseStatus.Cancelled)
        };

        var mockCourseRepo = MockHelpers.CreateMockCourseRepository(courses);
        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);

        // Act
        var result = await _service.GetCountAsync(CourseStatus.Active, null, null, null);

        // Assert
        Assert.Equal(2, result);
    }

    [Fact]
    public async Task GetCountAsync_WithNoMatches_ReturnsZero()
    {
        // Arrange
        var instrument = CreateInstrument();
        var courseType = CreateCourseType(instrument);
        var teacher = CreateTeacher();
        var courses = new List<Course>
        {
            CreateCourse(teacher, courseType, CourseStatus.Active)
        };

        var mockCourseRepo = MockHelpers.CreateMockCourseRepository(courses);
        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);

        // Act
        var result = await _service.GetCountAsync(CourseStatus.Cancelled, null, null, null);

        // Assert
        Assert.Equal(0, result);
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_WithNoFilters_ReturnsAllCourses()
    {
        // Arrange
        var instrument = CreateInstrument();
        var courseType = CreateCourseType(instrument);
        var teacher = CreateTeacher();
        var courses = new List<Course>
        {
            CreateCourse(teacher, courseType),
            CreateCourse(teacher, courseType, dayOfWeek: DayOfWeek.Wednesday)
        };

        var mockCourseRepo = MockHelpers.CreateMockCourseRepository(courses);
        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);

        // Act
        var result = await _service.GetAllAsync(null, null, null, null);

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetAllAsync_WithStatusFilter_ReturnsMatchingCourses()
    {
        // Arrange
        var instrument = CreateInstrument();
        var courseType = CreateCourseType(instrument);
        var teacher = CreateTeacher();
        var courses = new List<Course>
        {
            CreateCourse(teacher, courseType, CourseStatus.Active),
            CreateCourse(teacher, courseType, CourseStatus.Cancelled)
        };

        var mockCourseRepo = MockHelpers.CreateMockCourseRepository(courses);
        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);

        // Act
        var result = await _service.GetAllAsync(CourseStatus.Active, null, null, null);

        // Assert
        Assert.Single(result);
        Assert.Equal(CourseStatus.Active, result[0].Status);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsCourseDto()
    {
        // Arrange
        var instrument = CreateInstrument();
        var courseType = CreateCourseType(instrument);
        var teacher = CreateTeacher();
        var course = CreateCourse(teacher, courseType);
        var courses = new List<Course> { course };

        var mockCourseRepo = MockHelpers.CreateMockCourseRepository(courses);
        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);

        // Act
        var result = await _service.GetByIdAsync(course.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(course.Id, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var courses = new List<Course>();
        var mockCourseRepo = MockHelpers.CreateMockCourseRepository(courses);
        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);

        // Act
        var result = await _service.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_WithEnrollments_ReturnsCourseWithEnrollments()
    {
        // Arrange
        var instrument = CreateInstrument();
        var courseType = CreateCourseType(instrument);
        var teacher = CreateTeacher();
        var student = CreateStudent();
        var course = CreateCourse(teacher, courseType);
        course.Enrollments = new List<Enrollment>
        {
            new()
            {
                Id = Guid.NewGuid(),
                StudentId = student.Id,
                Student = student,
                CourseId = course.Id,
                Status = EnrollmentStatus.Active,
                EnrolledAt = DateTime.UtcNow
            }
        };
        var courses = new List<Course> { course };

        var mockCourseRepo = MockHelpers.CreateMockCourseRepository(courses);
        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);

        // Act
        var result = await _service.GetByIdAsync(course.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Enrollments);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidData_ReturnsCourseDto()
    {
        // Arrange
        var instrument = CreateInstrument();
        var courseType = CreateCourseType(instrument);
        var teacher = CreateTeacher();
        var courses = new List<Course>();

        var mockCourseRepo = MockHelpers.CreateMockCourseRepository(courses);
        var mockTeacherRepo = MockHelpers.CreateMockTeacherRepository(new List<Teacher> { teacher });
        var mockCourseTypeRepo = MockHelpers.CreateMockRepository(new List<CourseType> { courseType });

        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);
        _mockUnitOfWork.Setup(u => u.Teachers).Returns(mockTeacherRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<CourseType>()).Returns(mockCourseTypeRepo.Object);

        mockCourseTypeRepo.Setup(r => r.GetByIdAsync(courseType.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(courseType);

        mockCourseRepo.Setup(r => r.GetWithEnrollmentsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) =>
            {
                return new Course
                {
                    Id = id,
                    TeacherId = teacher.Id,
                    Teacher = teacher,
                    CourseTypeId = courseType.Id,
                    CourseType = courseType,
                    DayOfWeek = DayOfWeek.Monday,
                    StartTime = new TimeOnly(9, 0),
                    EndTime = new TimeOnly(10, 0),
                    Frequency = CourseFrequency.Weekly,
                    StartDate = DateOnly.FromDateTime(DateTime.Today),
                    Status = CourseStatus.Active,
                    Enrollments = new List<Enrollment>()
                };
            });

        var dto = new CreateCourseDto
        {
            TeacherId = teacher.Id,
            CourseTypeId = courseType.Id,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 0),
            Frequency = CourseFrequency.Weekly,
            StartDate = DateOnly.FromDateTime(DateTime.Today)
        };

        // Act
        var (course, error) = await _service.CreateAsync(dto);

        // Assert
        Assert.NotNull(course);
        Assert.Null(error);
        Assert.Equal(teacher.Id, course.TeacherId);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidTeacher_ReturnsError()
    {
        // Arrange
        var teachers = new List<Teacher>();
        var mockTeacherRepo = MockHelpers.CreateMockTeacherRepository(teachers);
        _mockUnitOfWork.Setup(u => u.Teachers).Returns(mockTeacherRepo.Object);

        var dto = new CreateCourseDto
        {
            TeacherId = Guid.NewGuid(),
            CourseTypeId = Guid.NewGuid(),
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 0),
            StartDate = DateOnly.FromDateTime(DateTime.Today)
        };

        // Act
        var (course, error) = await _service.CreateAsync(dto);

        // Assert
        Assert.Null(course);
        Assert.Equal("Teacher not found", error);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidCourseType_ReturnsError()
    {
        // Arrange
        var teacher = CreateTeacher();
        var mockTeacherRepo = MockHelpers.CreateMockTeacherRepository(new List<Teacher> { teacher });
        var mockCourseTypeRepo = MockHelpers.CreateMockRepository(new List<CourseType>());

        _mockUnitOfWork.Setup(u => u.Teachers).Returns(mockTeacherRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<CourseType>()).Returns(mockCourseTypeRepo.Object);

        mockCourseTypeRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CourseType?)null);

        var dto = new CreateCourseDto
        {
            TeacherId = teacher.Id,
            CourseTypeId = Guid.NewGuid(),
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 0),
            StartDate = DateOnly.FromDateTime(DateTime.Today)
        };

        // Act
        var (course, error) = await _service.CreateAsync(dto);

        // Assert
        Assert.Null(course);
        Assert.Equal("Course type not found", error);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidData_ReturnsUpdatedCourse()
    {
        // Arrange
        var instrument = CreateInstrument();
        var courseType = CreateCourseType(instrument);
        var teacher = CreateTeacher();
        var course = CreateCourse(teacher, courseType);
        var courses = new List<Course> { course };

        var mockCourseRepo = MockHelpers.CreateMockCourseRepository(courses);
        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);

        mockCourseRepo.Setup(r => r.GetWithEnrollmentsAsync(course.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) =>
            {
                course.DayOfWeek = DayOfWeek.Wednesday;
                return course;
            });

        var dto = new UpdateCourseDto
        {
            TeacherId = teacher.Id,
            CourseTypeId = courseType.Id,
            DayOfWeek = DayOfWeek.Wednesday,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0),
            Frequency = CourseFrequency.Weekly,
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            Status = CourseStatus.Active
        };

        // Act
        var (updated, notFound) = await _service.UpdateAsync(course.Id, dto);

        // Assert
        Assert.NotNull(updated);
        Assert.False(notFound);
        Assert.Equal(DayOfWeek.Wednesday, updated.DayOfWeek);
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var courses = new List<Course>();
        var mockCourseRepo = MockHelpers.CreateMockCourseRepository(courses);
        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);

        var dto = new UpdateCourseDto
        {
            TeacherId = Guid.NewGuid(),
            CourseTypeId = Guid.NewGuid(),
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 0),
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            Status = CourseStatus.Active
        };

        // Act
        var (updated, notFound) = await _service.UpdateAsync(Guid.NewGuid(), dto);

        // Assert
        Assert.Null(updated);
        Assert.True(notFound);
    }

    [Fact]
    public async Task UpdateAsync_StatusChange_UpdatesStatus()
    {
        // Arrange
        var instrument = CreateInstrument();
        var courseType = CreateCourseType(instrument);
        var teacher = CreateTeacher();
        var course = CreateCourse(teacher, courseType, CourseStatus.Active);
        var courses = new List<Course> { course };

        var mockCourseRepo = MockHelpers.CreateMockCourseRepository(courses);
        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);

        mockCourseRepo.Setup(r => r.GetWithEnrollmentsAsync(course.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) =>
            {
                course.Status = CourseStatus.Completed;
                return course;
            });

        var dto = new UpdateCourseDto
        {
            TeacherId = teacher.Id,
            CourseTypeId = courseType.Id,
            DayOfWeek = course.DayOfWeek,
            StartTime = course.StartTime,
            EndTime = course.EndTime,
            Frequency = course.Frequency,
            StartDate = course.StartDate,
            Status = CourseStatus.Completed
        };

        // Act
        var (updated, notFound) = await _service.UpdateAsync(course.Id, dto);

        // Assert
        Assert.NotNull(updated);
        Assert.Equal(CourseStatus.Completed, updated.Status);
    }

    #endregion

    #region EnrollStudentAsync Tests

    [Fact]
    public async Task EnrollStudentAsync_WithValidData_ReturnsEnrollment()
    {
        // Arrange
        var instrument = CreateInstrument();
        var courseType = CreateCourseType(instrument);
        var teacher = CreateTeacher();
        var student = CreateStudent();
        var course = CreateCourse(teacher, courseType);
        var courses = new List<Course> { course };
        var students = new List<Student> { student };
        var enrollments = new List<Enrollment>();

        var mockCourseRepo = MockHelpers.CreateMockCourseRepository(courses);
        var mockStudentRepo = MockHelpers.CreateMockStudentRepository(students);
        var mockEnrollmentRepo = MockHelpers.CreateMockRepository(enrollments);

        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);
        _mockUnitOfWork.Setup(u => u.Students).Returns(mockStudentRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<Enrollment>()).Returns(mockEnrollmentRepo.Object);

        var dto = new CreateEnrollmentDto
        {
            StudentId = student.Id,
            DiscountPercent = 10,
            Notes = "Test enrollment"
        };

        // Act
        var (enrollment, notFound, error) = await _service.EnrollStudentAsync(course.Id, dto);

        // Assert
        Assert.NotNull(enrollment);
        Assert.False(notFound);
        Assert.Null(error);
        Assert.Equal(student.Id, enrollment.StudentId);
        Assert.Equal(course.Id, enrollment.CourseId);
    }

    [Fact]
    public async Task EnrollStudentAsync_WithInvalidCourse_ReturnsNotFound()
    {
        // Arrange
        var courses = new List<Course>();
        var mockCourseRepo = MockHelpers.CreateMockCourseRepository(courses);
        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);

        var dto = new CreateEnrollmentDto
        {
            StudentId = Guid.NewGuid()
        };

        // Act
        var (enrollment, notFound, error) = await _service.EnrollStudentAsync(Guid.NewGuid(), dto);

        // Assert
        Assert.Null(enrollment);
        Assert.True(notFound);
    }

    [Fact]
    public async Task EnrollStudentAsync_WithInvalidStudent_ReturnsError()
    {
        // Arrange
        var instrument = CreateInstrument();
        var courseType = CreateCourseType(instrument);
        var teacher = CreateTeacher();
        var course = CreateCourse(teacher, courseType);
        var courses = new List<Course> { course };
        var students = new List<Student>();

        var mockCourseRepo = MockHelpers.CreateMockCourseRepository(courses);
        var mockStudentRepo = MockHelpers.CreateMockStudentRepository(students);

        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);
        _mockUnitOfWork.Setup(u => u.Students).Returns(mockStudentRepo.Object);

        var dto = new CreateEnrollmentDto
        {
            StudentId = Guid.NewGuid()
        };

        // Act
        var (enrollment, notFound, error) = await _service.EnrollStudentAsync(course.Id, dto);

        // Assert
        Assert.Null(enrollment);
        Assert.False(notFound);
        Assert.Equal("Student not found", error);
    }

    [Fact]
    public async Task EnrollStudentAsync_WhenAlreadyEnrolled_ReturnsError()
    {
        // Arrange
        var instrument = CreateInstrument();
        var courseType = CreateCourseType(instrument);
        var teacher = CreateTeacher();
        var student = CreateStudent();
        var course = CreateCourse(teacher, courseType);
        var existingEnrollment = new Enrollment
        {
            Id = Guid.NewGuid(),
            StudentId = student.Id,
            CourseId = course.Id,
            Status = EnrollmentStatus.Active
        };
        var courses = new List<Course> { course };
        var students = new List<Student> { student };
        var enrollments = new List<Enrollment> { existingEnrollment };

        var mockCourseRepo = MockHelpers.CreateMockCourseRepository(courses);
        var mockStudentRepo = MockHelpers.CreateMockStudentRepository(students);
        var mockEnrollmentRepo = MockHelpers.CreateMockRepository(enrollments);

        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);
        _mockUnitOfWork.Setup(u => u.Students).Returns(mockStudentRepo.Object);
        _mockUnitOfWork.Setup(u => u.Repository<Enrollment>()).Returns(mockEnrollmentRepo.Object);

        var dto = new CreateEnrollmentDto
        {
            StudentId = student.Id
        };

        // Act
        var (enrollment, notFound, error) = await _service.EnrollStudentAsync(course.Id, dto);

        // Assert
        Assert.Null(enrollment);
        Assert.False(notFound);
        Assert.Equal("Student is already enrolled in this course", error);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithValidId_ReturnsTrueAndSetsCancelled()
    {
        // Arrange
        var instrument = CreateInstrument();
        var courseType = CreateCourseType(instrument);
        var teacher = CreateTeacher();
        var course = CreateCourse(teacher, courseType);
        var courses = new List<Course> { course };

        var mockCourseRepo = MockHelpers.CreateMockCourseRepository(courses);
        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);

        // Act
        var result = await _service.DeleteAsync(course.Id);

        // Assert
        Assert.True(result);
        Assert.Equal(CourseStatus.Cancelled, course.Status);
    }

    [Fact]
    public async Task DeleteAsync_WithInvalidId_ReturnsFalse()
    {
        // Arrange
        var courses = new List<Course>();
        var mockCourseRepo = MockHelpers.CreateMockCourseRepository(courses);
        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);

        // Act
        var result = await _service.DeleteAsync(Guid.NewGuid());

        // Assert
        Assert.False(result);
    }

    #endregion
}
