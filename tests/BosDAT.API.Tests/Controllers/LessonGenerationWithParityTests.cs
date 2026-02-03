using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using BosDAT.Core.Utilities;
using Xunit;

namespace BosDAT.API.Tests.Controllers;

/// <summary>
/// Tests for lesson generation logic with ISO week parity support.
/// These test the static helper methods that will be used in LessonsController.
/// </summary>
public class LessonGenerationWithParityTests
{
    [Fact]
    public void GetNextOccurrenceDate_BiweeklyOddParity_ShouldSkipEvenWeeks()
    {
        // Arrange
        var course = new Course
        {
            Frequency = CourseFrequency.Biweekly,
            WeekParity = WeekParity.Odd
        };

        // Start in Week 1 of 2024 (odd week) - Monday, Jan 1, 2024
        var currentDate = new DateOnly(2024, 1, 1);

        // Act
        var nextDate = LessonGenerationHelper.GetNextOccurrenceDate(currentDate, course);

        // Assert
        // Should jump to Week 3 (next odd week), which is Monday, Jan 15, 2024
        Assert.Equal(new DateOnly(2024, 1, 15), nextDate);

        // Verify it's in an odd week
        var nextWeekNumber = IsoDateHelper.GetIsoWeekNumber(nextDate.ToDateTime(TimeOnly.MinValue));
        Assert.True(nextWeekNumber % 2 == 1, $"Week {nextWeekNumber} should be odd");
    }

    [Fact]
    public void GetNextOccurrenceDate_BiweeklyEvenParity_ShouldSkipOddWeeks()
    {
        // Arrange
        var course = new Course
        {
            Frequency = CourseFrequency.Biweekly,
            WeekParity = WeekParity.Even
        };

        // Start in Week 2 of 2024 (even week) - Monday, Jan 8, 2024
        var currentDate = new DateOnly(2024, 1, 8);

        // Act
        var nextDate = LessonGenerationHelper.GetNextOccurrenceDate(currentDate, course);

        // Assert
        // Should jump to Week 4 (next even week), which is Monday, Jan 22, 2024
        Assert.Equal(new DateOnly(2024, 1, 22), nextDate);

        // Verify it's in an even week
        var nextWeekNumber = IsoDateHelper.GetIsoWeekNumber(nextDate.ToDateTime(TimeOnly.MinValue));
        Assert.True(nextWeekNumber % 2 == 0, $"Week {nextWeekNumber} should be even");
    }

    [Fact]
    public void GetNextOccurrenceDate_BiweeklyAllParity_ShouldJump14Days()
    {
        // Arrange
        var course = new Course
        {
            Frequency = CourseFrequency.Biweekly,
            WeekParity = WeekParity.All
        };

        var currentDate = new DateOnly(2024, 1, 1);

        // Act
        var nextDate = LessonGenerationHelper.GetNextOccurrenceDate(currentDate, course);

        // Assert
        // Should simply add 14 days
        Assert.Equal(currentDate.AddDays(14), nextDate);
    }

    [Fact]
    public void GetNextOccurrenceDate_Weekly_ShouldJump7Days()
    {
        // Arrange
        var course = new Course
        {
            Frequency = CourseFrequency.Weekly,
            WeekParity = WeekParity.All
        };

        var currentDate = new DateOnly(2024, 1, 1);

        // Act
        var nextDate = LessonGenerationHelper.GetNextOccurrenceDate(currentDate, course);

        // Assert
        Assert.Equal(currentDate.AddDays(7), nextDate);
    }

    [Fact]
    public void FindFirstOccurrenceDate_BiweeklyOddParity_StartsInOddWeek_ShouldReturnSameDate()
    {
        // Arrange
        var course = new Course
        {
            DayOfWeek = DayOfWeek.Monday,
            Frequency = CourseFrequency.Biweekly,
            WeekParity = WeekParity.Odd
        };

        // Week 1 of 2024 (odd week) - Monday, Jan 1, 2024
        var startDate = new DateOnly(2024, 1, 1);
        var endDate = new DateOnly(2024, 12, 31);

        // Act
        var firstDate = LessonGenerationHelper.FindFirstOccurrenceDate(startDate, course, endDate);

        // Assert
        Assert.Equal(new DateOnly(2024, 1, 1), firstDate);
    }

    [Fact]
    public void FindFirstOccurrenceDate_BiweeklyOddParity_StartsInEvenWeek_ShouldJumpToNextOddWeek()
    {
        // Arrange
        var course = new Course
        {
            DayOfWeek = DayOfWeek.Monday,
            Frequency = CourseFrequency.Biweekly,
            WeekParity = WeekParity.Odd
        };

        // Week 2 of 2024 (even week) - Monday, Jan 8, 2024
        var startDate = new DateOnly(2024, 1, 8);
        var endDate = new DateOnly(2024, 12, 31);

        // Act
        var firstDate = LessonGenerationHelper.FindFirstOccurrenceDate(startDate, course, endDate);

        // Assert
        // Should jump to Week 3 (odd week) - Monday, Jan 15, 2024
        Assert.Equal(new DateOnly(2024, 1, 15), firstDate);

        var weekNumber = IsoDateHelper.GetIsoWeekNumber(firstDate.ToDateTime(TimeOnly.MinValue));
        Assert.True(weekNumber % 2 == 1, $"Week {weekNumber} should be odd");
    }

    [Fact]
    public void FindFirstOccurrenceDate_BiweeklyEvenParity_StartsInOddWeek_ShouldJumpToNextEvenWeek()
    {
        // Arrange
        var course = new Course
        {
            DayOfWeek = DayOfWeek.Monday,
            Frequency = CourseFrequency.Biweekly,
            WeekParity = WeekParity.Even
        };

        // Week 1 of 2024 (odd week) - Monday, Jan 1, 2024
        var startDate = new DateOnly(2024, 1, 1);
        var endDate = new DateOnly(2024, 12, 31);

        // Act
        var firstDate = LessonGenerationHelper.FindFirstOccurrenceDate(startDate, course, endDate);

        // Assert
        // Should jump to Week 2 (even week) - Monday, Jan 8, 2024
        Assert.Equal(new DateOnly(2024, 1, 8), firstDate);

        var weekNumber = IsoDateHelper.GetIsoWeekNumber(firstDate.ToDateTime(TimeOnly.MinValue));
        Assert.True(weekNumber % 2 == 0, $"Week {weekNumber} should be even");
    }

    [Fact]
    public void GenerateLessons_BiweeklyOdd_ShouldOnlyCreateInOddWeeks()
    {
        // Arrange
        var course = new Course
        {
            DayOfWeek = DayOfWeek.Monday,
            Frequency = CourseFrequency.Biweekly,
            WeekParity = WeekParity.Odd
        };

        var startDate = new DateOnly(2024, 1, 1); // Week 1 (odd)
        var endDate = new DateOnly(2024, 2, 29);   // ~8 weeks

        // Act
        var lessonDates = LessonGenerationHelper.GenerateLessonDates(startDate, endDate, course);

        // Assert
        Assert.NotEmpty(lessonDates);

        // Verify all dates are in odd weeks
        foreach (var date in lessonDates)
        {
            var weekNumber = IsoDateHelper.GetIsoWeekNumber(date.ToDateTime(TimeOnly.MinValue));
            Assert.True(weekNumber % 2 == 1, $"Date {date} is in week {weekNumber}, which is not odd");
        }

        // Verify we have approximately the right number of lessons (4 odd weeks in ~8 weeks)
        Assert.InRange(lessonDates.Count, 4, 5);
    }

    [Fact]
    public void GenerateLessons_BiweeklyEven_ShouldOnlyCreateInEvenWeeks()
    {
        // Arrange
        var course = new Course
        {
            DayOfWeek = DayOfWeek.Monday,
            Frequency = CourseFrequency.Biweekly,
            WeekParity = WeekParity.Even
        };

        var startDate = new DateOnly(2024, 1, 1); // Week 1 (odd) - should skip to week 2
        var endDate = new DateOnly(2024, 2, 29);   // ~8 weeks

        // Act
        var lessonDates = LessonGenerationHelper.GenerateLessonDates(startDate, endDate, course);

        // Assert
        Assert.NotEmpty(lessonDates);

        // Verify all dates are in even weeks
        foreach (var date in lessonDates)
        {
            var weekNumber = IsoDateHelper.GetIsoWeekNumber(date.ToDateTime(TimeOnly.MinValue));
            Assert.True(weekNumber % 2 == 0, $"Date {date} is in week {weekNumber}, which is not even");
        }

        // Verify we have approximately the right number of lessons (4 even weeks in ~8 weeks)
        Assert.InRange(lessonDates.Count, 3, 5);
    }

    [Fact]
    public void GenerateLessons_Across53WeekYear_ShouldHandleCorrectly()
    {
        // 2026 is a 53-week year. Week 53 of 2026 and Week 1 of 2027 are both odd.
        // This test verifies the system handles this edge case.

        // Arrange
        var course = new Course
        {
            DayOfWeek = DayOfWeek.Monday,
            Frequency = CourseFrequency.Biweekly,
            WeekParity = WeekParity.Odd
        };

        // Start in late December 2026, cross into 2027
        var startDate = new DateOnly(2026, 12, 21); // Week 52 (even)
        var endDate = new DateOnly(2027, 1, 31);

        // Act
        var lessonDates = LessonGenerationHelper.GenerateLessonDates(startDate, endDate, course);

        // Assert
        Assert.NotEmpty(lessonDates);

        // Verify all dates are in odd weeks
        foreach (var date in lessonDates)
        {
            var weekNumber = IsoDateHelper.GetIsoWeekNumber(date.ToDateTime(TimeOnly.MinValue));
            Assert.True(weekNumber % 2 == 1, $"Date {date} is in week {weekNumber}, which is not odd");
        }

        // Should have Week 53 of 2026 (Dec 28) and Week 1 of 2027 (Jan 4)
        // These are 7 days apart (not 14) due to 53-week year
        Assert.Contains(new DateOnly(2026, 12, 28), lessonDates);
        Assert.Contains(new DateOnly(2027, 1, 4), lessonDates);
    }
}

/// <summary>
/// Helper class to encapsulate lesson generation logic for testing.
/// This will be moved into LessonsController as static methods.
/// </summary>
public static class LessonGenerationHelper
{
    public static DateOnly GetNextOccurrenceDate(DateOnly currentDate, Course course)
    {
        // For biweekly with specific parity, we need to find the next occurrence in a matching week
        if (course.Frequency == CourseFrequency.Biweekly && course.WeekParity != WeekParity.All)
        {
            // Start by adding 7 days (1 week)
            var nextDate = currentDate.AddDays(7);

            // Keep adding weeks until we find one that matches the parity
            while (!IsoDateHelper.MatchesWeekParity(nextDate.ToDateTime(TimeOnly.MinValue), course.WeekParity))
            {
                nextDate = nextDate.AddDays(7);
            }

            return nextDate;
        }

        // For other frequencies, use simple date arithmetic
        return course.Frequency switch
        {
            CourseFrequency.Weekly => currentDate.AddDays(7),
            CourseFrequency.Biweekly => currentDate.AddDays(14),
            CourseFrequency.Monthly => currentDate.AddMonths(1),
            _ => currentDate.AddDays(7)
        };
    }

    public static DateOnly FindFirstOccurrenceDate(DateOnly startDate, Course course, DateOnly endDate)
    {
        var currentDate = startDate;

        // Find first occurrence of the target day of week
        while (currentDate.DayOfWeek != course.DayOfWeek && currentDate <= endDate)
        {
            currentDate = currentDate.AddDays(1);
        }

        // For biweekly with specific parity, ensure we start on correct week
        if (course.Frequency == CourseFrequency.Biweekly && course.WeekParity != WeekParity.All)
        {
            while (!IsoDateHelper.MatchesWeekParity(currentDate.ToDateTime(TimeOnly.MinValue), course.WeekParity)
                   && currentDate <= endDate)
            {
                currentDate = currentDate.AddDays(7);
            }
        }

        return currentDate;
    }

    public static List<DateOnly> GenerateLessonDates(DateOnly startDate, DateOnly endDate, Course course)
    {
        var dates = new List<DateOnly>();
        var currentDate = FindFirstOccurrenceDate(startDate, course, endDate);

        while (currentDate <= endDate)
        {
            dates.Add(currentDate);
            currentDate = GetNextOccurrenceDate(currentDate, course);
        }

        return dates;
    }
}
