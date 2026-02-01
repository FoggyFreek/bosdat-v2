using Microsoft.AspNetCore.Mvc;
using Xunit;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using static BosDAT.API.Tests.Controllers.LessonsController.TestHelpers;

namespace BosDAT.API.Tests.Controllers.LessonsController;

/// <summary>
/// Tests for holiday skipping functionality in the lesson generation algorithm.
/// Verifies that lessons are correctly skipped when they fall within holiday date ranges,
/// handles single-day and multi-day holidays, multiple holidays, and respects the SkipHolidays flag.
/// </summary>
public class HolidaySkippingTests : LessonGenerationTestBase
{

    [Fact]
    public async Task GenerateLessons_SingleDayHoliday_SkipsOneLesson()
    {
        // Arrange
        var course = new CourseBuilder()
            .WithFrequency(CourseFrequency.Weekly)
            .WithDayOfWeek(DayOfWeek.Monday)
            .Build();

        var holidays = new List<Holiday>
        {
            CreateHoliday(1, "Single Day Holiday", new DateOnly(2024, 3, 11), new DateOnly(2024, 3, 11))
        };
        SetupMocks(course, new List<Lesson>(), holidays);

        var dto = CreateGenerateDto(
            course.Id,
            new DateOnly(2024, 3, 4),  // Monday
            new DateOnly(2024, 3, 25), // Monday
            skipHolidays: true);

        // Act
        var result = await Controller.GenerateLessons(dto, CancellationToken.None);

        // Assert
        AssertGenerationResult(result, expectedCreated: 3, expectedSkipped: 1);
    }

    [Fact]
    public async Task GenerateLessons_MultiDayHoliday_SkipsAllOverlappingLessons()
    {
        // Arrange
        var course = new CourseBuilder()
            .WithFrequency(CourseFrequency.Weekly)
            .WithDayOfWeek(DayOfWeek.Monday)
            .Build();

        var holidays = new List<Holiday>
        {
            CreateHoliday(1, "Two-Week Holiday", new DateOnly(2024, 3, 11), new DateOnly(2024, 3, 22))
        };
        SetupMocks(course, new List<Lesson>(), holidays);

        var dto = CreateGenerateDto(
            course.Id,
            new DateOnly(2024, 3, 4),
            new DateOnly(2024, 3, 25),
            skipHolidays: true);

        // Act
        var result = await Controller.GenerateLessons(dto, CancellationToken.None);

        // Assert
        // 4 Mondays total, 2 fall within holiday (March 11, 18)
        AssertGenerationResult(result, expectedCreated: 2, expectedSkipped: 2);
    }

    [Fact]
    public async Task GenerateLessons_HolidayOutsideRange_NoSkips()
    {
        // Arrange
        var course = new CourseBuilder()
            .WithFrequency(CourseFrequency.Weekly)
            .WithDayOfWeek(DayOfWeek.Monday)
            .Build();

        var holidays = new List<Holiday>
        {
            CreateHoliday(1, "December Holiday", new DateOnly(2024, 12, 23), new DateOnly(2024, 12, 31))
        };
        SetupMocks(course, new List<Lesson>(), holidays);

        var dto = CreateGenerateDto(
            course.Id,
            new DateOnly(2024, 3, 4),
            new DateOnly(2024, 3, 25),
            skipHolidays: true);

        // Act
        var result = await Controller.GenerateLessons(dto, CancellationToken.None);

        // Assert
        AssertGenerationResult(result, expectedCreated: 4, expectedSkipped: 0);
    }

    [Fact]
    public async Task GenerateLessons_SkipHolidaysFalse_IgnoresAllHolidays()
    {
        // Arrange
        var course = new CourseBuilder()
            .WithFrequency(CourseFrequency.Weekly)
            .WithDayOfWeek(DayOfWeek.Monday)
            .Build();

        var holidays = new List<Holiday>
        {
            CreateHoliday(1, "Spring Break", new DateOnly(2024, 3, 11), new DateOnly(2024, 3, 15))
        };
        SetupMocks(course, new List<Lesson>(), holidays);

        var dto = CreateGenerateDto(
            course.Id,
            new DateOnly(2024, 3, 4),
            new DateOnly(2024, 3, 25),
            skipHolidays: false);  // Don't skip holidays

        // Act
        var result = await Controller.GenerateLessons(dto, CancellationToken.None);

        // Assert
        AssertGenerationResult(result, expectedCreated: 4, expectedSkipped: 0);
    }

    [Fact]
    public async Task GenerateLessons_MultipleHolidays_SkipsAll()
    {
        // Arrange
        var course = new CourseBuilder()
            .WithFrequency(CourseFrequency.Weekly)
            .WithDayOfWeek(DayOfWeek.Monday)
            .Build();

        var holidays = new List<Holiday>
        {
            CreateHoliday(1, "Holiday 1", new DateOnly(2024, 3, 11), new DateOnly(2024, 3, 11)),
            CreateHoliday(2, "Holiday 2", new DateOnly(2024, 3, 25), new DateOnly(2024, 3, 25))
        };
        SetupMocks(course, new List<Lesson>(), holidays);

        var dto = CreateGenerateDto(
            course.Id,
            new DateOnly(2024, 3, 4),
            new DateOnly(2024, 3, 25),
            skipHolidays: true);

        // Act
        var result = await Controller.GenerateLessons(dto, CancellationToken.None);

        // Assert
        AssertGenerationResult(result, expectedCreated: 2, expectedSkipped: 2);
    }

    #region Helper Methods

    /// <summary>
    /// Creates a holiday entity with the specified properties.
    /// </summary>
    private static Holiday CreateHoliday(int id, string name, DateOnly startDate, DateOnly endDate)
    {
        return new Holiday
        {
            Id = id,
            Name = name,
            StartDate = startDate,
            EndDate = endDate
        };
    }

    #endregion
}
