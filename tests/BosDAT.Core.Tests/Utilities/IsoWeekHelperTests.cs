using System.Globalization;
using BosDAT.Core.Enums;
using BosDAT.Core.Utilities;
using Xunit;

namespace BosDAT.Core.Tests.Utilities;

public class IsoWeekHelperTests
{
    [Theory]
    [InlineData(2024, 1, 1, 1)]   // Monday, Jan 1, 2024 is Week 1
    [InlineData(2024, 12, 30, 1)] // Monday, Dec 30, 2024 is Week 1 of 2025
    [InlineData(2025, 1, 6, 2)]   // Monday, Jan 6, 2025 is Week 2
    [InlineData(2025, 12, 29, 1)] // Monday, Dec 29, 2025 is Week 1 of 2026
    [InlineData(2026, 1, 1, 1)]   // Thursday, Jan 1, 2026 is Week 1 of 2026
    [InlineData(2026, 12, 31, 53)] // Thursday, Dec 31, 2026 is Week 53 of 2026
    [InlineData(2027, 1, 1, 53)]  // Friday, Jan 1, 2027 is Week 53 of 2026
    public void GetIsoWeekNumber_ShouldReturnCorrectWeekNumber(int year, int month, int day, int expectedWeek)
    {
        // Arrange
        var date = new DateTime(year, month, day);

        // Act
        var weekNumber = IsoWeekHelper.GetIsoWeekNumber(date);

        // Assert
        Assert.Equal(expectedWeek, weekNumber);
    }

    [Theory]
    [InlineData(2024, 1, 1, 2024)]  // Week 1 of 2024
    [InlineData(2024, 12, 30, 2025)] // Week 1 of 2025 (Dec 30, 2024)
    [InlineData(2025, 12, 29, 2026)] // Week 1 of 2026 (Dec 29, 2025)
    [InlineData(2026, 1, 1, 2026)]   // Week 1 of 2026 (Jan 1, 2026)
    [InlineData(2027, 1, 1, 2026)]   // Week 53 of 2026 (Jan 1, 2027)
    public void GetIsoWeekYear_ShouldReturnCorrectYear(int year, int month, int day, int expectedIsoYear)
    {
        // Arrange
        var date = new DateTime(year, month, day);

        // Act
        var isoYear = IsoWeekHelper.GetIsoWeekYear(date);

        // Assert
        Assert.Equal(expectedIsoYear, isoYear);
    }

    [Theory]
    [InlineData(2024, false)] // Regular year (52 weeks)
    [InlineData(2025, false)] // Regular year (52 weeks)
    [InlineData(2026, true)]  // 53-week year (starts on Thursday)
    [InlineData(2032, true)]  // 53-week year (leap year starting on Thursday)
    [InlineData(2037, true)]  // 53-week year (starts on Thursday)
    [InlineData(2043, true)]  // 53-week year (starts on Thursday)
    public void Is53WeekYear_ShouldReturnCorrectValue(int isoYear, bool expected)
    {
        // Act
        var is53Week = IsoWeekHelper.Is53WeekYear(isoYear);

        // Assert
        Assert.Equal(expected, is53Week);
    }

    [Theory]
    [InlineData(2024, 1, 1, WeekParity.Odd)]   // Week 1 (odd)
    [InlineData(2024, 1, 8, WeekParity.Even)]  // Week 2 (even)
    [InlineData(2024, 1, 15, WeekParity.Odd)]  // Week 3 (odd)
    [InlineData(2025, 12, 29, WeekParity.Odd)] // Week 1 of 2026 (odd)
    [InlineData(2026, 1, 1, WeekParity.Odd)]   // Week 1 of 2026 (odd)
    [InlineData(2026, 12, 31, WeekParity.Odd)] // Week 53 of 2026 (odd)
    [InlineData(2027, 1, 1, WeekParity.Odd)]   // Week 53 of 2026 (odd)
    public void GetWeekParity_ShouldReturnCorrectParity(int year, int month, int day, WeekParity expected)
    {
        // Arrange
        var date = new DateTime(year, month, day);

        // Act
        var parity = IsoWeekHelper.GetWeekParity(date);

        // Assert
        Assert.Equal(expected, parity);
    }

    [Theory]
    [InlineData(2024, 1, 1, WeekParity.All, true)]   // All always matches
    [InlineData(2024, 1, 1, WeekParity.Odd, true)]   // Week 1 matches odd
    [InlineData(2024, 1, 1, WeekParity.Even, false)] // Week 1 doesn't match even
    [InlineData(2024, 1, 8, WeekParity.Odd, false)]  // Week 2 doesn't match odd
    [InlineData(2024, 1, 8, WeekParity.Even, true)]  // Week 2 matches even
    [InlineData(2024, 1, 8, WeekParity.All, true)]   // All always matches
    public void MatchesWeekParity_ShouldReturnCorrectResult(int year, int month, int day, WeekParity parity, bool expected)
    {
        // Arrange
        var date = new DateTime(year, month, day);

        // Act
        var matches = IsoWeekHelper.MatchesWeekParity(date, parity);

        // Assert
        Assert.Equal(expected, matches);
    }

    [Fact]
    public void GetIsoWeekNumber_ShouldMatchSystemGlobalizationISOWeek()
    {
        // Arrange - test across a full year including edge cases
        var testDates = new[]
        {
            new DateTime(2024, 1, 1),
            new DateTime(2024, 6, 15),
            new DateTime(2024, 12, 31),
            new DateTime(2025, 1, 1),
            new DateTime(2026, 1, 1),
            new DateTime(2026, 12, 31),
            new DateTime(2027, 1, 1)
        };

        foreach (var date in testDates)
        {
            // Act
            var helperWeek = IsoWeekHelper.GetIsoWeekNumber(date);
            var systemWeek = ISOWeek.GetWeekOfYear(date);

            // Assert
            Assert.Equal(systemWeek, helperWeek);
        }
    }

    [Fact]
    public void GetIsoWeekYear_ShouldMatchSystemGlobalizationISOWeek()
    {
        // Arrange - test year boundary edge cases
        var testDates = new[]
        {
            new DateTime(2024, 1, 1),
            new DateTime(2024, 12, 30),
            new DateTime(2025, 1, 1),
            new DateTime(2025, 12, 29),
            new DateTime(2026, 1, 1),
            new DateTime(2027, 1, 1)
        };

        foreach (var date in testDates)
        {
            // Act
            var helperYear = IsoWeekHelper.GetIsoWeekYear(date);
            var systemYear = ISOWeek.GetYear(date);

            // Assert
            Assert.Equal(systemYear, helperYear);
        }
    }

    [Fact]
    public void Is53WeekYear_2026_ShouldHandleCorrectly()
    {
        // 2026 is a 53-week year (starts on Thursday)
        // Week 53 of 2026 contains Dec 28, 2026 - Jan 3, 2027
        // Week 1 of 2027 starts Jan 4, 2027

        // Arrange
        var lastDayOfWeek53 = new DateTime(2027, 1, 3); // Sunday
        var firstDayOfWeek1 = new DateTime(2027, 1, 4); // Monday

        // Act
        var week53Number = IsoWeekHelper.GetIsoWeekNumber(lastDayOfWeek53);
        var week53Year = IsoWeekHelper.GetIsoWeekYear(lastDayOfWeek53);
        var week1Number = IsoWeekHelper.GetIsoWeekNumber(firstDayOfWeek1);
        var week1Year = IsoWeekHelper.GetIsoWeekYear(firstDayOfWeek1);

        // Assert
        Assert.Equal(53, week53Number);
        Assert.Equal(2026, week53Year); // Week 53 belongs to 2026
        Assert.Equal(1, week1Number);
        Assert.Equal(2027, week1Year);
    }

    [Fact]
    public void GetWeekParity_Week53AndWeek1_BothOddIn2026Transition()
    {
        // 2026 is a 53-week year, both week 53 (of 2026) and week 1 (of 2027) are odd
        // This creates two consecutive odd weeks (7-day gap instead of 14)

        // Arrange
        var week53Date = new DateTime(2026, 12, 31); // Thursday, Week 53 of 2026
        var week1Date = new DateTime(2027, 1, 4);    // Monday, Week 1 of 2027

        // Act
        var week53Parity = IsoWeekHelper.GetWeekParity(week53Date);
        var week1Parity = IsoWeekHelper.GetWeekParity(week1Date);

        // Assert
        Assert.Equal(WeekParity.Odd, week53Parity);
        Assert.Equal(WeekParity.Odd, week1Parity);
    }
}
