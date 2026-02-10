using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Infrastructure.Repositories;

namespace BosDAT.Infrastructure.Tests.Repositories;

public class EnrollmentRepositoryTests : RepositoryTestBase
{
    private readonly EnrollmentRepository _repository;

    public EnrollmentRepositoryTests()
    {
        _repository = new EnrollmentRepository(Context);
        SeedTestData();
    }

    [Fact]
    public async Task GetActiveEnrollmentsByStudentIdAsync_ShouldReturnActiveEnrollments()
    {
        // Arrange
        var student = Context.Students.First(s => s.Email == "alice.johnson@example.com");

        // Act
        var result = await _repository.GetActiveEnrollmentsByStudentIdAsync(student.Id);

        // Assert
        Assert.NotEmpty(result);
        Assert.All(result, e =>
        {
            Assert.Equal(student.Id, e.StudentId);
            Assert.Contains(e.Status, new[] { EnrollmentStatus.Active, EnrollmentStatus.Trail });
        });
    }

    [Fact]
    public async Task GetActiveEnrollmentsByStudentIdAsync_ShouldIncludeTrailEnrollments()
    {
        // Arrange
        var student = Context.Students.First(s => s.Email == "bob.williams@example.com");

        // Act
        var result = await _repository.GetActiveEnrollmentsByStudentIdAsync(student.Id);

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains(result, e => e.Status == EnrollmentStatus.Trail);
    }

    [Fact]
    public async Task GetActiveEnrollmentsByStudentIdAsync_ShouldIncludeRelatedData()
    {
        // Arrange
        var student = Context.Students.First(s => s.Email == "alice.johnson@example.com");

        // Act
        var result = await _repository.GetActiveEnrollmentsByStudentIdAsync(student.Id);

        // Assert
        Assert.NotEmpty(result);
        var enrollment = result.First();
        Assert.NotNull(enrollment.Course);
        Assert.NotNull(enrollment.Course.CourseType);
        Assert.NotNull(enrollment.Course.Teacher);
    }

    [Fact]
    public async Task GetActiveEnrollmentsByStudentIdAsync_ShouldNotReturnCompletedEnrollments()
    {
        // Arrange
        var student = Context.Students.First();
        var course = Context.Courses.First();

        var completedEnrollment = new Enrollment
        {
            Id = Guid.NewGuid(),
            StudentId = student.Id,
            CourseId = course.Id,
            EnrolledAt = new DateTime(2023, 1, 1),
            Status = EnrollmentStatus.Completed
        };
        Context.Enrollments.Add(completedEnrollment);
        await Context.SaveChangesAsync();

        // Act
        var result = await _repository.GetActiveEnrollmentsByStudentIdAsync(student.Id);

        // Assert
        Assert.DoesNotContain(result, e => e.Id == completedEnrollment.Id);
    }

    [Fact]
    public async Task GetActiveEnrollmentsByStudentIdAsync_ShouldNotReturnCancelledEnrollments()
    {
        // Arrange
        var student = Context.Students.First();
        var course = Context.Courses.First();

        var cancelledEnrollment = new Enrollment
        {
            Id = Guid.NewGuid(),
            StudentId = student.Id,
            CourseId = course.Id,
            EnrolledAt = new DateTime(2023, 1, 1),
            Status = EnrollmentStatus.Withdrawn
        };
        Context.Enrollments.Add(cancelledEnrollment);
        await Context.SaveChangesAsync();

        // Act
        var result = await _repository.GetActiveEnrollmentsByStudentIdAsync(student.Id);

        // Assert
        Assert.DoesNotContain(result, e => e.Id == cancelledEnrollment.Id);
    }

    [Fact]
    public async Task GetActiveEnrollmentsByStudentIdAsync_ShouldReturnEmptyForStudentWithNoEnrollments()
    {
        // Arrange
        var studentWithNoEnrollments = new Student
        {
            Id = Guid.NewGuid(),
            FirstName = "No",
            LastName = "Enrollments",
            Email = "noenrollments@example.com",
            Phone = "0600000000",
            DateOfBirth = new DateOnly(2010, 1, 1),
            Status = StudentStatus.Active
        };
        Context.Students.Add(studentWithNoEnrollments);
        await Context.SaveChangesAsync();

        // Act
        var result = await _repository.GetActiveEnrollmentsByStudentIdAsync(studentWithNoEnrollments.Id);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetActiveEnrollmentsByStudentIdAsync_ShouldReturnEmptyForNonexistentStudent()
    {
        // Arrange
        var nonexistentStudentId = Guid.NewGuid();

        // Act
        var result = await _repository.GetActiveEnrollmentsByStudentIdAsync(nonexistentStudentId);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetActiveEnrollmentsByStudentIdAsync_ShouldHandleMultipleActiveEnrollments()
    {
        // Arrange
        var student = Context.Students.First();
        var courses = Context.Courses.Take(2).ToList();

        var enrollment1 = new Enrollment
        {
            Id = Guid.NewGuid(),
            StudentId = student.Id,
            CourseId = courses[0].Id,
            EnrolledAt = new DateTime(2024, 1, 1),
            Status = EnrollmentStatus.Active
        };

        var enrollment2 = new Enrollment
        {
            Id = Guid.NewGuid(),
            StudentId = student.Id,
            CourseId = courses[1].Id,
            EnrolledAt = new DateTime(2024, 1, 15),
            Status = EnrollmentStatus.Active
        };

        Context.Enrollments.AddRange(enrollment1, enrollment2);
        await Context.SaveChangesAsync();

        // Act
        var result = await _repository.GetActiveEnrollmentsByStudentIdAsync(student.Id);

        // Assert
        Assert.Contains(result, e => e.Id == enrollment1.Id);
        Assert.Contains(result, e => e.Id == enrollment2.Id);
    }
}
