using System.Globalization;
using BosDAT.Core.Enums;
using BosDAT.Core.Utilities;
using Xunit;

namespace BosDAT.Core.Tests.Utilities;

public class IsoDateHelperTests
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
        var weekNumber = IsoDateHelper.GetIsoWeekNumber(date);

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
        var isoYear = IsoDateHelper.GetIsoWeekYear(date);

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
        var is53Week = IsoDateHelper.Is53WeekYear(isoYear);

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
        var parity = IsoDateHelper.GetWeekParity(date);

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
        var matches = IsoDateHelper.MatchesWeekParity(date, parity);

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
            var helperWeek = IsoDateHelper.GetIsoWeekNumber(date);
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
            var helperYear = IsoDateHelper.GetIsoWeekYear(date);
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
        var week53Number = IsoDateHelper.GetIsoWeekNumber(lastDayOfWeek53);
        var week53Year = IsoDateHelper.GetIsoWeekYear(lastDayOfWeek53);
        var week1Number = IsoDateHelper.GetIsoWeekNumber(firstDayOfWeek1);
        var week1Year = IsoDateHelper.GetIsoWeekYear(firstDayOfWeek1);

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
        var week53Parity = IsoDateHelper.GetWeekParity(week53Date);
        var week1Parity = IsoDateHelper.GetWeekParity(week1Date);

        // Assert
        Assert.Equal(WeekParity.Odd, week53Parity);
        Assert.Equal(WeekParity.Odd, week1Parity);
    }

    [Fact]
    public void Is53WeekYear_53WeekYear_ShouldReturnTrue()
    {
        // This test verifies that the Is53WeekYear method correctly uses
        // the native ISOWeek.GetWeeksInYear method to detect 53-week years.

        // Arrange - 2026 is a 53-week year (starts on Thursday)
        var year = 2026;

        // Act
        var is53Week = IsoDateHelper.Is53WeekYear(year);

        // Assert
        Assert.True(is53Week);
    }

    [Fact]
    public void Is53WeekYear_RegularYear_ShouldReturnFalse()
    {
        // This test verifies that the Is53WeekYear method correctly uses
        // the native ISOWeek.GetWeeksInYear method to detect regular (52-week) years.

        // Arrange - 2024 is a regular year (52 weeks)
        var year = 2024;

        // Act
        var is53Week = IsoDateHelper.Is53WeekYear(year);

        // Assert
        Assert.False(is53Week);
    }

    #region Date/Time Conversion Tests

    [Fact]
    public void ToDateTimeUtc_DateOnly_ShouldConvertToMidnightUtc()
    {
        // Arrange
        var date = new DateOnly(2026, 2, 15);

        // Act
        var dateTime = IsoDateHelper.ToDateTimeUtc(date);

        // Assert
        Assert.Equal(2026, dateTime.Year);
        Assert.Equal(2, dateTime.Month);
        Assert.Equal(15, dateTime.Day);
        Assert.Equal(0, dateTime.Hour);
        Assert.Equal(0, dateTime.Minute);
        Assert.Equal(0, dateTime.Second);
        Assert.Equal(DateTimeKind.Utc, dateTime.Kind);
    }

    [Fact]
    public void ToDateTimeUtc_DateOnlyAndTimeOnly_ShouldConvertCorrectly()
    {
        // Arrange
        var date = new DateOnly(2026, 2, 15);
        var time = new TimeOnly(19, 30, 0);

        // Act
        var dateTime = IsoDateHelper.ToDateTimeUtc(date, time);

        // Assert
        Assert.Equal(2026, dateTime.Year);
        Assert.Equal(2, dateTime.Month);
        Assert.Equal(15, dateTime.Day);
        Assert.Equal(19, dateTime.Hour);
        Assert.Equal(30, dateTime.Minute);
        Assert.Equal(0, dateTime.Second);
        Assert.Equal(DateTimeKind.Utc, dateTime.Kind);
    }

    [Fact]
    public void TodayUtc_ShouldReturnCurrentDateInUtc()
    {
        // Arrange
        var expectedDate = DateOnly.FromDateTime(DateTime.UtcNow);

        // Act
        var actualDate = IsoDateHelper.TodayUtc();

        // Assert
        Assert.Equal(expectedDate, actualDate);
    }

    [Fact]
    public void ToDateOnly_ShouldExtractDateFromDateTime()
    {
        // Arrange
        var dateTime = new DateTime(2026, 2, 15, 19, 30, 0);

        // Act
        var dateOnly = IsoDateHelper.ToDateOnly(dateTime);

        // Assert
        Assert.Equal(new DateOnly(2026, 2, 15), dateOnly);
    }

    #endregion

    #region ISO DateTime String Formatting Tests

    [Theory]
    [InlineData("02-15-2026", "19:30:00", "2026-02-15T19:30:00Z")]
    [InlineData("02-15-2026", "19:30:00:00", "2026-02-15T19:30:00Z")]
    [InlineData("01-01-2024", "00:00:00", "2024-01-01T00:00:00Z")]
    [InlineData("12-31-2025", "23:59:59", "2025-12-31T23:59:59Z")]
    public void CreateIsoDateTime_StringDateAndTime_ShouldFormatCorrectly(string date, string time, string expected)
    {
        // Act
        var result = IsoDateHelper.CreateIsoDateTime(date, time);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void CreateIsoDateTime_InvalidDateFormat_ShouldThrowFormatException()
    {
        // Arrange
        var invalidDate = "2026-02-15"; // Wrong format
        var time = "19:30:00";

        // Act & Assert
        var exception = Assert.Throws<FormatException>(() =>
            IsoDateHelper.CreateIsoDateTime(invalidDate, time));
        Assert.Contains("Invalid date format", exception.Message);
    }

    [Fact]
    public void CreateIsoDateTime_InvalidTimeFormat_ShouldThrowFormatException()
    {
        // Arrange
        var date = "02-15-2026";
        var invalidTime = "7:30 PM"; // Wrong format

        // Act & Assert
        var exception = Assert.Throws<FormatException>(() =>
            IsoDateHelper.CreateIsoDateTime(date, invalidTime));
        Assert.Contains("Invalid time format", exception.Message);
    }

    [Fact]
    public void CreateIsoDateTime_DateOnlyAndTimeString_ShouldFormatCorrectly()
    {
        // Arrange
        var date = new DateOnly(2026, 2, 15);
        var time = "19:30:00";

        // Act
        var result = IsoDateHelper.CreateIsoDateTime(date, time);

        // Assert
        Assert.Equal("2026-02-15T19:30:00Z", result);
    }

    [Fact]
    public void CreateIsoDateTime_DateOnlyAndTimeOnly_ShouldFormatCorrectly()
    {
        // Arrange
        var date = new DateOnly(2026, 2, 15);
        var time = new TimeOnly(19, 30, 0);

        // Act
        var result = IsoDateHelper.CreateIsoDateTime(date, time);

        // Assert
        Assert.Equal("2026-02-15T19:30:00Z", result);
    }

    #endregion

    #region Age Calculation Tests

    [Theory]
    [InlineData(2000, 1, 1, 2026, 2, 3, 26)]     // 26 years old (birthday passed)
    [InlineData(2000, 3, 1, 2026, 2, 3, 25)]     // Still 25 (birthday not yet)
    [InlineData(2008, 2, 3, 2026, 2, 3, 18)]     // Exactly 18 today
    [InlineData(2008, 2, 4, 2026, 2, 3, 17)]     // Still 17 (birthday tomorrow)
    [InlineData(2020, 6, 15, 2026, 2, 3, 5)]     // 5 years old
    public void CalculateAge_ShouldReturnCorrectAge(int dobYear, int dobMonth, int dobDay,
        int refYear, int refMonth, int refDay, int expectedAge)
    {
        // Arrange
        var dateOfBirth = new DateOnly(dobYear, dobMonth, dobDay);
        var referenceDate = new DateOnly(refYear, refMonth, refDay);

        // Act
        var age = IsoDateHelper.CalculateAge(dateOfBirth, referenceDate);

        // Assert
        Assert.Equal(expectedAge, age);
    }

    [Fact]
    public void CalculateAge_WithoutReferenceDate_ShouldUseTodayUtc()
    {
        // Arrange
        var dateOfBirth = new DateOnly(2000, 1, 1);
        var expectedAge = DateTime.UtcNow.Year - 2000;

        // Adjust for birthday not yet occurred this year
        if (DateTime.UtcNow.Month == 1 && DateTime.UtcNow.Day < 1)
        {
            expectedAge--;
        }

        // Act
        var age = IsoDateHelper.CalculateAge(dateOfBirth);

        // Assert (allow for potential day boundary)
        Assert.True(age == expectedAge || age == expectedAge - 1);
    }

    [Theory]
    [InlineData(2010, 1, 1, 2026, 2, 3, 18, false)] // 16 years old, child
    [InlineData(2008, 1, 1, 2026, 2, 3, 18, false)] // Exactly 18, not a child
    [InlineData(2007, 12, 31, 2026, 2, 3, 18, false)] // 18+, not a child
    [InlineData(2015, 6, 15, 2026, 2, 3, 18, true)]  // 10 years old, child
    [InlineData(2010, 1, 1, 2026, 2, 3, 12, false)] // 16 years old, not child with limit 12
    [InlineData(2015, 1, 1, 2026, 2, 3, 12, true)]  // 11 years old, child with limit 12
    public void IsChild_ShouldDetermineCorrectly(int dobYear, int dobMonth, int dobDay,
        int refYear, int refMonth, int refDay, int ageLimit, bool expectedIsChild)
    {
        // Arrange
        var dateOfBirth = new DateOnly(dobYear, dobMonth, dobDay);
        var referenceDate = new DateOnly(refYear, refMonth, refDay);

        // Act
        var isChild = IsoDateHelper.IsChild(dateOfBirth, ageLimit, referenceDate);

        // Assert
        Assert.Equal(expectedIsChild, isChild);
    }

    [Fact]
    public void IsChild_NullDateOfBirth_ShouldReturnFalse()
    {
        // Arrange
        DateOnly? dateOfBirth = null;

        // Act
        var isChild = IsoDateHelper.IsChild(dateOfBirth);

        // Assert
        Assert.False(isChild);
    }

    #endregion

    #region Date Navigation Tests

    [Theory]
    [InlineData(2026, 2, 15, 2026, 2, 9)]   // Sunday -> previous Monday
    [InlineData(2026, 2, 9, 2026, 2, 9)]    // Monday -> same Monday
    [InlineData(2026, 2, 10, 2026, 2, 9)]   // Tuesday -> previous Monday
    [InlineData(2026, 2, 14, 2026, 2, 9)]   // Saturday -> previous Monday
    [InlineData(2024, 1, 1, 2024, 1, 1)]    // Monday, Jan 1 -> same day
    public void GetWeekStart_ShouldReturnMonday(int year, int month, int day,
        int expectedYear, int expectedMonth, int expectedDay)
    {
        // Arrange
        var date = new DateOnly(year, month, day);
        var expected = new DateOnly(expectedYear, expectedMonth, expectedDay);

        // Act
        var weekStart = IsoDateHelper.GetWeekStart(date);

        // Assert
        Assert.Equal(expected, weekStart);
        Assert.Equal(DayOfWeek.Monday, weekStart.DayOfWeek);
    }

    [Theory]
    [InlineData(2026, 2, 15, 2026, 2, 1)]   // Mid-month
    [InlineData(2026, 2, 1, 2026, 2, 1)]    // First day
    [InlineData(2026, 2, 28, 2026, 2, 1)]   // Last day
    [InlineData(2024, 2, 29, 2024, 2, 1)]   // Leap year last day
    public void GetMonthStart_ShouldReturnFirstDayOfMonth(int year, int month, int day,
        int expectedYear, int expectedMonth, int expectedDay)
    {
        // Arrange
        var date = new DateOnly(year, month, day);
        var expected = new DateOnly(expectedYear, expectedMonth, expectedDay);

        // Act
        var monthStart = IsoDateHelper.GetMonthStart(date);

        // Assert
        Assert.Equal(expected, monthStart);
        Assert.Equal(1, monthStart.Day);
    }

    [Theory]
    [InlineData(2026, 2, 15, 2026, 2, 28)]  // February non-leap year
    [InlineData(2024, 2, 1, 2024, 2, 29)]   // February leap year
    [InlineData(2026, 1, 15, 2026, 1, 31)]  // January (31 days)
    [InlineData(2026, 4, 10, 2026, 4, 30)]  // April (30 days)
    [InlineData(2026, 12, 1, 2026, 12, 31)] // December
    public void GetMonthEnd_ShouldReturnLastDayOfMonth(int year, int month, int day,
        int expectedYear, int expectedMonth, int expectedDay)
    {
        // Arrange
        var date = new DateOnly(year, month, day);
        var expected = new DateOnly(expectedYear, expectedMonth, expectedDay);

        // Act
        var monthEnd = IsoDateHelper.GetMonthEnd(date);

        // Assert
        Assert.Equal(expected, monthEnd);
    }

    #endregion
}
