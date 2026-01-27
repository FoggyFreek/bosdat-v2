using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using BosDAT.API.Controllers;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;
using BosDAT.API.Tests.Helpers;

namespace BosDAT.API.Tests.Controllers;

public class CoursesControllerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly CoursesController _controller;

    public CoursesControllerTests()
    {
        _mockUnitOfWork = MockHelpers.CreateMockUnitOfWork();
        _controller = new CoursesController(_mockUnitOfWork.Object);
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

    private static Course CreateCourse(
        Teacher teacher,
        CourseType courseType,
        CourseStatus status = CourseStatus.Active,
        DayOfWeek dayOfWeek = DayOfWeek.Monday)
    {
        return new Course
        {
            Id = Guid.NewGuid(),
            TeacherId = teacher.Id,
            Teacher = teacher,
            CourseTypeId = courseType.Id,
            CourseType = courseType,
            DayOfWeek = dayOfWeek,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 0),
            Frequency = CourseFrequency.Weekly,
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            Status = status,
            Enrollments = new List<Enrollment>()
        };
    }

    #region GetAll Tests

    [Fact]
    public async Task GetAll_WithNoFilters_ReturnsAllCourses()
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
        var result = await _controller.GetAll(null, null, null, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedCourses = Assert.IsAssignableFrom<IEnumerable<CourseListDto>>(okResult.Value);
        Assert.Equal(2, returnedCourses.Count());
    }

    [Fact]
    public async Task GetAll_WithStatusFilter_ReturnsMatchingCourses()
    {
        // Arrange
        var instrument = CreateInstrument();
        var courseType = CreateCourseType(instrument);
        var teacher = CreateTeacher();
        var activeCourse = CreateCourse(teacher, courseType, CourseStatus.Active);
        var cancelledCourse = CreateCourse(teacher, courseType, CourseStatus.Cancelled);
        var courses = new List<Course> { activeCourse, cancelledCourse };

        var mockCourseRepo = MockHelpers.CreateMockCourseRepository(courses);
        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);

        // Act
        var result = await _controller.GetAll(CourseStatus.Active, null, null, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedCourses = Assert.IsAssignableFrom<IEnumerable<CourseListDto>>(okResult.Value);
        Assert.Single(returnedCourses);
        Assert.Equal(CourseStatus.Active, returnedCourses.First().Status);
    }

    [Fact]
    public async Task GetAll_WithTeacherFilter_ReturnsMatchingCourses()
    {
        // Arrange
        var instrument = CreateInstrument();
        var courseType = CreateCourseType(instrument);
        var teacher1 = CreateTeacher("John", "Doe");
        var teacher2 = CreateTeacher("Jane", "Smith");
        var course1 = CreateCourse(teacher1, courseType);
        var course2 = CreateCourse(teacher2, courseType);
        var courses = new List<Course> { course1, course2 };

        var mockCourseRepo = MockHelpers.CreateMockCourseRepository(courses);
        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);

        // Act
        var result = await _controller.GetAll(null, teacher1.Id, null, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedCourses = Assert.IsAssignableFrom<IEnumerable<CourseListDto>>(okResult.Value);
        Assert.Single(returnedCourses);
    }

    [Fact]
    public async Task GetAll_WithDayOfWeekFilter_ReturnsMatchingCourses()
    {
        // Arrange
        var instrument = CreateInstrument();
        var courseType = CreateCourseType(instrument);
        var teacher = CreateTeacher();
        var mondayCourse = CreateCourse(teacher, courseType, dayOfWeek: DayOfWeek.Monday);
        var wednesdayCourse = CreateCourse(teacher, courseType, dayOfWeek: DayOfWeek.Wednesday);
        var courses = new List<Course> { mondayCourse, wednesdayCourse };

        var mockCourseRepo = MockHelpers.CreateMockCourseRepository(courses);
        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);

        // Act
        var result = await _controller.GetAll(null, null, DayOfWeek.Monday, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedCourses = Assert.IsAssignableFrom<IEnumerable<CourseListDto>>(okResult.Value);
        Assert.Single(returnedCourses);
        Assert.Equal(DayOfWeek.Monday, returnedCourses.First().DayOfWeek);
    }

    [Fact]
    public async Task GetAll_WithMultipleFilters_ReturnsMatchingCourses()
    {
        // Arrange
        var instrument = CreateInstrument();
        var courseType = CreateCourseType(instrument);
        var teacher = CreateTeacher();
        var activeMondayCourse = CreateCourse(teacher, courseType, CourseStatus.Active, DayOfWeek.Monday);
        var cancelledMondayCourse = CreateCourse(teacher, courseType, CourseStatus.Cancelled, DayOfWeek.Monday);
        var activeWednesdayCourse = CreateCourse(teacher, courseType, CourseStatus.Active, DayOfWeek.Wednesday);
        var courses = new List<Course> { activeMondayCourse, cancelledMondayCourse, activeWednesdayCourse };

        var mockCourseRepo = MockHelpers.CreateMockCourseRepository(courses);
        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);

        // Act
        var result = await _controller.GetAll(CourseStatus.Active, teacher.Id, DayOfWeek.Monday, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedCourses = Assert.IsAssignableFrom<IEnumerable<CourseListDto>>(okResult.Value);
        Assert.Single(returnedCourses);
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WithValidId_ReturnsCourse()
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
        var result = await _controller.GetById(course.Id, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedCourse = Assert.IsType<CourseDto>(okResult.Value);
        Assert.Equal(course.Id, returnedCourse.Id);
    }

    [Fact]
    public async Task GetById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var courses = new List<Course>();
        var mockCourseRepo = MockHelpers.CreateMockCourseRepository(courses);
        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);

        // Act
        var result = await _controller.GetById(Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetById_WithEnrollments_ReturnsCourseWithEnrollments()
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
        var result = await _controller.GetById(course.Id, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedCourse = Assert.IsType<CourseDto>(okResult.Value);
        Assert.Single(returnedCourse.Enrollments);
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_WithValidData_ReturnsCreatedCourse()
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

        // Setup to return the created course
        mockCourseRepo.Setup(r => r.GetWithEnrollmentsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) =>
            {
                var created = new Course
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
                return created;
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
        var result = await _controller.Create(dto, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(CoursesController.GetById), createdResult.ActionName);
        var returnedCourse = Assert.IsType<CourseDto>(createdResult.Value);
        Assert.Equal(teacher.Id, returnedCourse.TeacherId);
    }

    [Fact]
    public async Task Create_WithInvalidTeacher_ReturnsBadRequest()
    {
        // Arrange
        var courseType = CreateCourseType(CreateInstrument());
        var teachers = new List<Teacher>();

        var mockTeacherRepo = MockHelpers.CreateMockTeacherRepository(teachers);
        _mockUnitOfWork.Setup(u => u.Teachers).Returns(mockTeacherRepo.Object);

        var dto = new CreateCourseDto
        {
            TeacherId = Guid.NewGuid(),
            CourseTypeId = courseType.Id,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 0),
            StartDate = DateOnly.FromDateTime(DateTime.Today)
        };

        // Act
        var result = await _controller.Create(dto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task Create_WithInvalidCourseType_ReturnsBadRequest()
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
        var result = await _controller.Create(dto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_WithValidData_ReturnsUpdatedCourse()
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
        var result = await _controller.Update(course.Id, dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedCourse = Assert.IsType<CourseDto>(okResult.Value);
        Assert.Equal(DayOfWeek.Wednesday, returnedCourse.DayOfWeek);
    }

    [Fact]
    public async Task Update_WithInvalidId_ReturnsNotFound()
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
        var result = await _controller.Update(Guid.NewGuid(), dto, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Update_StatusChange_UpdatesStatus()
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
        var result = await _controller.Update(course.Id, dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedCourse = Assert.IsType<CourseDto>(okResult.Value);
        Assert.Equal(CourseStatus.Completed, returnedCourse.Status);
    }

    #endregion

    #region EnrollStudent Tests

    [Fact]
    public async Task EnrollStudent_WithValidData_ReturnsEnrollment()
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
        var result = await _controller.EnrollStudent(course.Id, dto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedEnrollment = Assert.IsType<EnrollmentDto>(okResult.Value);
        Assert.Equal(student.Id, returnedEnrollment.StudentId);
        Assert.Equal(course.Id, returnedEnrollment.CourseId);
    }

    [Fact]
    public async Task EnrollStudent_WithInvalidCourse_ReturnsNotFound()
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
        var result = await _controller.EnrollStudent(Guid.NewGuid(), dto, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task EnrollStudent_WithInvalidStudent_ReturnsBadRequest()
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
        var result = await _controller.EnrollStudent(course.Id, dto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task EnrollStudent_WhenAlreadyEnrolled_ReturnsBadRequest()
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
        var result = await _controller.EnrollStudent(course.Id, dto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_WithValidId_ReturnsNoContent()
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
        var result = await _controller.Delete(course.Id, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        Assert.Equal(CourseStatus.Cancelled, course.Status);
    }

    [Fact]
    public async Task Delete_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var courses = new List<Course>();
        var mockCourseRepo = MockHelpers.CreateMockCourseRepository(courses);
        _mockUnitOfWork.Setup(u => u.Courses).Returns(mockCourseRepo.Object);

        // Act
        var result = await _controller.Delete(Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    #endregion
}
