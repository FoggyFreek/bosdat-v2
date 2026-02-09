using Microsoft.EntityFrameworkCore;
using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Infrastructure.Data;

namespace BosDAT.Infrastructure.Tests.Repositories;

public abstract class RepositoryTestBase : IDisposable
{
    protected readonly ApplicationDbContext Context;

    protected RepositoryTestBase()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"RepositoryTest_{Guid.NewGuid()}")
            .Options;

        Context = new ApplicationDbContext(options);
    }

    protected void SeedTestData()
    {
        // Seed Instruments
        var piano = new Instrument { Id = 1, Name = "Piano", Category = InstrumentCategory.Keyboard };
        var guitar = new Instrument { Id = 2, Name = "Guitar", Category = InstrumentCategory.String };
        Context.Instruments.AddRange(piano, guitar);

        // Seed Rooms
        var room1 = new Room { Id = 1, Name = "Room 1", Capacity = 1, IsActive = true };
        var room2 = new Room { Id = 2, Name = "Room 2", Capacity = 10, IsActive = true };
        Context.Rooms.AddRange(room1, room2);

        // Seed Teachers
        var teacher1 = new Teacher
        {
            Id = Guid.NewGuid(),
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Phone = "0612345678",
            IsActive = true
        };
        var teacher2 = new Teacher
        {
            Id = Guid.NewGuid(),
            FirstName = "Jane",
            LastName = "Smith",
            Email = "jane.smith@example.com",
            Phone = "0687654321",
            IsActive = true
        };
        Context.Teachers.AddRange(teacher1, teacher2);

        // Seed Students
        var student1 = new Student
        {
            Id = Guid.NewGuid(),
            FirstName = "Alice",
            LastName = "Johnson",
            Email = "alice.johnson@example.com",
            Phone = "0611111111",
            DateOfBirth = new DateOnly(2010, 5, 15),
            Status = StudentStatus.Active
        };
        var student2 = new Student
        {
            Id = Guid.NewGuid(),
            FirstName = "Bob",
            LastName = "Williams",
            Email = "bob.williams@example.com",
            Phone = "0622222222",
            DateOfBirth = new DateOnly(2012, 8, 20),
            Status = StudentStatus.Active
        };
        Context.Students.AddRange(student1, student2);

        // Seed Course Types
        var individualCourseType = new CourseType
        {
            Id = Guid.NewGuid(),
            Name = "Individual Piano",
            Type = CourseTypeCategory.Individual,
            InstrumentId = 1,
            DurationMinutes = 30,
            IsActive = true
        };
        var groupCourseType = new CourseType
        {
            Id = Guid.NewGuid(),
            Name = "Group Guitar",
            Type = CourseTypeCategory.Group,
            InstrumentId = 2,
            DurationMinutes = 60,
            IsActive = true
        };
        Context.CourseTypes.AddRange(individualCourseType, groupCourseType);

        // Seed Courses
        var course1 = new Course
        {
            Id = Guid.NewGuid(),
            CourseTypeId = individualCourseType.Id,
            TeacherId = teacher1.Id,
            RoomId = 1,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(14, 0),
            EndTime = new TimeOnly(14, 30),
            StartDate = new DateOnly(2024, 1, 1),
            EndDate = new DateOnly(2024, 12, 31),
            Status = CourseStatus.Active,
            Frequency = CourseFrequency.Weekly
        };
        var course2 = new Course
        {
            Id = Guid.NewGuid(),
            CourseTypeId = groupCourseType.Id,
            TeacherId = teacher2.Id,
            RoomId = 2,
            DayOfWeek = DayOfWeek.Wednesday,
            StartTime = new TimeOnly(16, 0),
            EndTime = new TimeOnly(17, 0),
            StartDate = new DateOnly(2024, 1, 1),
            EndDate = new DateOnly(2024, 12, 31),
            Status = CourseStatus.Active,
            Frequency = CourseFrequency.Biweekly,
            WeekParity = WeekParity.Even
        };
        Context.Courses.AddRange(course1, course2);

        // Seed Enrollments
        var enrollment1 = new Enrollment
        {
            Id = Guid.NewGuid(),
            StudentId = student1.Id,
            CourseId = course1.Id,
            EnrolledAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Status = EnrollmentStatus.Active
        };
        var enrollment2 = new Enrollment
        {
            Id = Guid.NewGuid(),
            StudentId = student2.Id,
            CourseId = course2.Id,
            EnrolledAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Status = EnrollmentStatus.Trail
        };
        Context.Enrollments.AddRange(enrollment1, enrollment2);

        Context.SaveChanges();
    }

    public void Dispose()
    {
        Context.Database.EnsureDeleted();
        Context.Dispose();
        GC.SuppressFinalize(this);
    }
}
