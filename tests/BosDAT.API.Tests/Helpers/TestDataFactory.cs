using BosDAT.Core.Entities;
using BosDAT.Core.Enums;

namespace BosDAT.API.Tests.Helpers;

public static class TestDataFactory
{
    public static Student CreateStudent(string firstName = "Jane", string lastName = "Smith")
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

    public static Teacher CreateTeacher(string firstName = "John", string lastName = "Doe")
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

    public static Instrument CreateInstrument(int id = 1, string name = "Piano")
    {
        return new Instrument
        {
            Id = id,
            Name = name,
            Category = InstrumentCategory.Keyboard,
            IsActive = true
        };
    }

    public static CourseType CreateCourseType(Instrument instrument, string name = "Beginner Piano")
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

    public static Room CreateRoom(int id = 1, string name = "Room 1")
    {
        return new Room
        {
            Id = id,
            Name = name,
            Capacity = 2,
            IsActive = true
        };
    }

    public static Course CreateCourse(
        Teacher teacher,
        CourseType courseType,
        CourseStatus status = CourseStatus.Active,
        DayOfWeek dayOfWeek = DayOfWeek.Monday,
        Room? room = null,
        bool isTrial = false)
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
            IsTrial = isTrial,
            Enrollments = new List<Enrollment>()
        };
    }

    public static Enrollment CreateEnrollment(
        Student student,
        Course course,
        EnrollmentStatus status = EnrollmentStatus.Active)
    {
        return new Enrollment
        {
            Id = Guid.NewGuid(),
            StudentId = student.Id,
            Student = student,
            CourseId = course.Id,
            Course = course,
            Status = status,
            EnrolledAt = DateTime.UtcNow,
            DiscountPercent = 0,
            InvoicingPreference = InvoicingPreference.Monthly
        };
    }
}
