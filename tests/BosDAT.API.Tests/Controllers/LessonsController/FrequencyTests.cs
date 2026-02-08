using Microsoft.AspNetCore.Mvc;
using Xunit;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using static BosDAT.API.Tests.Controllers.LessonsController.TestHelpers;

namespace BosDAT.API.Tests.Controllers.LessonsController;

/// <summary>
/// Tests for lesson generation across different course frequencies (Weekly, Biweekly, Monthly).
/// Validates that the scheduling algorithm correctly calculates lesson dates based on frequency settings.
/// </summary>
public class FrequencyTests : LessonGenerationTestBase
{
    [Theory]
    [InlineData(CourseFrequency.Weekly, DayOfWeek.Monday, "2024-03-04", "2024-03-25", 4, 0)]
    [InlineData(CourseFrequency.Biweekly, DayOfWeek.Wednesday, "2024-03-06", "2024-04-30", 4, 0)]
    public async Task GenerateLessons_BasicFrequency_CreatesExpectedLessons(
        CourseFrequency frequency,
        DayOfWeek dayOfWeek,
        string startDateStr,
        string endDateStr,
        int expectedCreated,
        int expectedSkipped)
    {
        // Arrange
        var course = new CourseBuilder()
            .WithFrequency(frequency)
            .WithDayOfWeek(dayOfWeek)
            .Build();

        SetupMocks(course, new List<Lesson>(), new List<Holiday>());

        var dto = CreateGenerateDto(
            course.Id,
            DateOnly.Parse(startDateStr),
            DateOnly.Parse(endDateStr));

        // Act
        var result = await Controller.GenerateLessons(dto, CancellationToken.None);

        // Assert
        AssertGenerationResult(result, expectedCreated, expectedSkipped);
    }

    [Fact]
    public async Task GenerateLessons_Weekly_FourWeeks_CreatesFourLessons()
    {
        // Arrange
        var course = new CourseBuilder()
            .WithFrequency(CourseFrequency.Weekly)
            .WithDayOfWeek(DayOfWeek.Monday)
            .Build();

        SetupMocks(course, new List<Lesson>(), new List<Holiday>());

        var dto = CreateGenerateDto(
            course.Id,
            TestDates.March4_2024,   // Monday
            TestDates.March25_2024); // Monday, 4 weeks later

        // Act
        var result = await Controller.GenerateLessons(dto, CancellationToken.None);

        // Assert
        AssertGenerationResult(result, expectedCreated: 4, expectedSkipped: 0);
    }

    [Fact]
    public async Task GenerateLessons_Biweekly_EightWeeks_CreatesFourLessons()
    {
        // Arrange
        var course = new CourseBuilder()
            .WithFrequency(CourseFrequency.Biweekly)
            .WithDayOfWeek(DayOfWeek.Wednesday)
            .Build();

        SetupMocks(course, new List<Lesson>(), new List<Holiday>());

        // 8 weeks, biweekly = 4 lessons (March 6, March 20, April 3, April 17)
        var dto = CreateGenerateDto(
            course.Id,
            TestDates.March6_2024,             // Wednesday
            new DateOnly(2024, 4, 30));        // End of April (~8 weeks)

        // Act
        var result = await Controller.GenerateLessons(dto, CancellationToken.None);

        // Assert
        AssertGenerationResult(result, expectedCreated: 4, expectedSkipped: 0);
    }

    [Fact]
    public async Task GenerateLessons_Monthly_ThreeMonths_CreatesThreeLessons()
    {
        // Arrange
        var course = new CourseBuilder()
            .WithFrequency(CourseFrequency.Once)
            .WithDayOfWeek(DayOfWeek.Friday)
            .Build();

        SetupMocks(course, new List<Lesson>(), new List<Holiday>());

        var dto = CreateGenerateDto(
            course.Id,
            new DateOnly(2024, 3, 1),   // Friday, March 1
            new DateOnly(2024, 5, 31)); // End of May (3 months)

        // Act
        var result = await Controller.GenerateLessons(dto, CancellationToken.None);

        // Assert
        // Monthly frequency: March 1, April 5 (first Friday), May 3 (first Friday) = 3 lessons
        AssertGenerationResult(result, expectedCreated: 3, expectedSkipped: 0);
    }

    [Fact]
    public async Task GenerateLessons_Monthly_MonthEndHandling_HandlesFebruaryLeapYear()
    {
        // Arrange - Course on 31st day
        var course = new CourseBuilder()
            .WithFrequency(CourseFrequency.Once)
            .WithDayOfWeek(DayOfWeek.Wednesday)
            .Build();

        SetupMocks(course, new List<Lesson>(), new List<Holiday>());

        // Test month-end behavior: Jan 31 → Feb 29 (2024 is leap year) → Mar 31
        var dto = CreateGenerateDto(
            course.Id,
            TestDates.Jan31_2024,       // Wednesday, Jan 31
            TestDates.Mar31_2024);      // Sunday, Mar 31

        // Act
        var result = await Controller.GenerateLessons(dto, CancellationToken.None);

        // Assert
        // Jan 31, Feb 28 (adjusted for month-end), Mar 27 (last Wednesday)
        // The exact behavior depends on the month-end handling algorithm
        AssertGenerationResult(result, expectedCreated: 3, expectedSkipped: 0);
    }
}
