using System.Globalization;
using BosDAT.Core.Enums;

namespace BosDAT.Core.Utilities;

/// <summary>
/// Utility class for ISO 8601 date/time operations including week-based calculations,
/// date conversions, and ISO datetime string formatting.
/// </summary>
public static class IsoDateHelper
{
    /// <summary>
    /// Gets the ISO week number for a given date.
    /// </summary>
    /// <param name="date">The date to get the week number for.</param>
    /// <returns>The ISO week number (1-53).</returns>
    public static int GetIsoWeekNumber(DateTime date)
    {
        return ISOWeek.GetWeekOfYear(date);
    }

    /// <summary>
    /// Gets the ISO week-numbering year for a given date.
    /// Note: This may differ from the calendar year for dates in late December or early January.
    /// </summary>
    /// <param name="date">The date to get the ISO year for.</param>
    /// <returns>The ISO week-numbering year.</returns>
    public static int GetIsoWeekYear(DateTime date)
    {
        return ISOWeek.GetYear(date);
    }

    /// <summary>
    /// Determines if a given ISO year has 53 weeks.
    /// An ISO year has 53 weeks if:
    /// - It starts on a Thursday (January 1 is a Thursday), OR
    /// - It's a leap year starting on a Wednesday (January 1 is a Wednesday in a leap year)
    /// </summary>
    /// <param name="isoYear">The ISO year to check.</param>
    /// <returns>True if the year has 53 weeks, false otherwise.</returns>
    public static bool Is53WeekYear(int isoYear)
    {
        // Get the last day of the ISO year
        // If week 53 exists, Dec 31 or a nearby date will be in week 53
        // DateTimeKind.Unspecified is used since ISO week calculations are calendar-based
        // and independent of timezone (per SonarQube rule csharpsquid:S6562)
        var dec31 = new DateTime(isoYear, 12, 31, 0, 0, 0, DateTimeKind.Unspecified);
        var isoWeekOfDec31 = GetIsoWeekNumber(dec31);
        var isoYearOfDec31 = GetIsoWeekYear(dec31);

        // If Dec 31 is in week 53 of the same ISO year, it's a 53-week year
        if (isoYearOfDec31 == isoYear && isoWeekOfDec31 == 53)
        {
            return true;
        }

        // Check a few days before Dec 31 (since Dec 31 might be in Week 1 of next year)
        var dec28 = new DateTime(isoYear, 12, 28, 0, 0, 0, DateTimeKind.Unspecified);
        var isoWeekOfDec28 = GetIsoWeekNumber(dec28);
        var isoYearOfDec28 = GetIsoWeekYear(dec28);

        return isoYearOfDec28 == isoYear && isoWeekOfDec28 == 53;
    }

    /// <summary>
    /// Gets the week parity (Odd or Even) for a given date based on its ISO week number.
    /// </summary>
    /// <param name="date">The date to get the parity for.</param>
    /// <returns>WeekParity.Odd for odd weeks (1, 3, 5, ...), WeekParity.Even for even weeks (2, 4, 6, ...).</returns>
    public static WeekParity GetWeekParity(DateTime date)
    {
        var weekNumber = GetIsoWeekNumber(date);
        return weekNumber % 2 == 1 ? WeekParity.Odd : WeekParity.Even;
    }

    /// <summary>
    /// Checks if a date matches a given week parity.
    /// </summary>
    /// <param name="date">The date to check.</param>
    /// <param name="parity">The week parity to match against.</param>
    /// <returns>True if the date matches the parity, false otherwise. WeekParity.All always returns true.</returns>
    public static bool MatchesWeekParity(DateTime date, WeekParity parity)
    {
        // All parity matches any week
        if (parity == WeekParity.All)
        {
            return true;
        }

        var dateParity = GetWeekParity(date);
        return dateParity == parity;
    }

    #region Date/Time Conversions

    /// <summary>
    /// Converts a DateOnly to DateTime with UTC kind at midnight.
    /// </summary>
    /// <param name="date">The date to convert.</param>
    /// <returns>DateTime with UTC kind at 00:00:00.</returns>
    public static DateTime ToDateTimeUtc(DateOnly date)
    {
        return date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
    }

    /// <summary>
    /// Converts a DateOnly and TimeOnly to DateTime with UTC kind.
    /// </summary>
    /// <param name="date">The date component.</param>
    /// <param name="time">The time component.</param>
    /// <returns>DateTime with UTC kind.</returns>
    public static DateTime ToDateTimeUtc(DateOnly date, TimeOnly time)
    {
        return date.ToDateTime(time, DateTimeKind.Utc);
    }

    /// <summary>
    /// Gets the current date as DateOnly in UTC.
    /// </summary>
    /// <returns>Today's date in UTC as DateOnly.</returns>
    public static DateOnly TodayUtc()
    {
        return DateOnly.FromDateTime(DateTime.UtcNow);
    }

    /// <summary>
    /// Converts DateTime to DateOnly.
    /// </summary>
    /// <param name="dateTime">The DateTime to convert.</param>
    /// <returns>The date component as DateOnly.</returns>
    public static DateOnly ToDateOnly(DateTime dateTime)
    {
        return DateOnly.FromDateTime(dateTime);
    }

    #endregion

    #region ISO DateTime String Formatting

    /// <summary>
    /// Creates an ISO 8601 datetime string from date and time components.
    /// Supports time formats: 'HH:mm:ss', 'HH:mm:ss:ff' (with fractional seconds treated as milliseconds).
    /// </summary>
    /// <param name="date">The date in MM-dd-yyyy format (e.g., "02-15-2026").</param>
    /// <param name="time">The time in HH:mm:ss or HH:mm:ss:ff format (e.g., "19:30:00" or "19:30:00:00").</param>
    /// <returns>ISO 8601 formatted datetime string (e.g., "2026-02-15T19:30:00Z").</returns>
    /// <exception cref="FormatException">Thrown when date or time format is invalid.</exception>
    public static string CreateIsoDateTime(string date, string time)
    {
        // Parse date in MM-dd-yyyy format
        if (!DateTime.TryParseExact(date, "MM-dd-yyyy", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var parsedDate))
        {
            throw new FormatException($"Invalid date format: '{date}'. Expected format: MM-dd-yyyy");
        }

        // Parse time - support both HH:mm:ss and HH:mm:ss:ff formats
        TimeOnly parsedTime;
        var timeFormats = new[] { "HH:mm:ss", "HH:mm:ss:ff" };

        if (!TimeOnly.TryParseExact(time, timeFormats, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out parsedTime))
        {
            throw new FormatException($"Invalid time format: '{time}'. Expected format: HH:mm:ss or HH:mm:ss:ff");
        }

        // Create UTC DateTime and format as ISO 8601
        var dateTime = new DateTime(parsedDate.Year, parsedDate.Month, parsedDate.Day,
            parsedTime.Hour, parsedTime.Minute, parsedTime.Second, DateTimeKind.Utc);

        return dateTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Creates an ISO 8601 datetime string from DateOnly and time string.
    /// </summary>
    /// <param name="date">The date component.</param>
    /// <param name="time">The time in HH:mm:ss or HH:mm:ss:ff format.</param>
    /// <returns>ISO 8601 formatted datetime string.</returns>
    public static string CreateIsoDateTime(DateOnly date, string time)
    {
        var dateString = date.ToString("MM-dd-yyyy", CultureInfo.InvariantCulture);
        return CreateIsoDateTime(dateString, time);
    }

    /// <summary>
    /// Creates an ISO 8601 datetime string from DateOnly and TimeOnly.
    /// </summary>
    /// <param name="date">The date component.</param>
    /// <param name="time">The time component.</param>
    /// <returns>ISO 8601 formatted datetime string.</returns>
    public static string CreateIsoDateTime(DateOnly date, TimeOnly time)
    {
        var dateTime = date.ToDateTime(time, DateTimeKind.Utc);
        return dateTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
    }

    #endregion

    #region Age Calculation

    /// <summary>
    /// Calculates age in years from a date of birth.
    /// </summary>
    /// <param name="dateOfBirth">The date of birth.</param>
    /// <param name="referenceDate">The reference date to calculate age from (defaults to today in UTC).</param>
    /// <returns>Age in completed years.</returns>
    public static int CalculateAge(DateOnly dateOfBirth, DateOnly? referenceDate = null)
    {
        var reference = referenceDate ?? TodayUtc();
        var age = reference.Year - dateOfBirth.Year;

        // Subtract one year if birthday hasn't occurred yet this year
        if (reference.Month < dateOfBirth.Month ||
            (reference.Month == dateOfBirth.Month && reference.Day < dateOfBirth.Day))
        {
            age--;
        }

        return age;
    }

    /// <summary>
    /// Determines if a person is a child (under specified age limit).
    /// </summary>
    /// <param name="dateOfBirth">The date of birth (null returns false).</param>
    /// <param name="ageLimit">The age limit to check against (default 18).</param>
    /// <param name="referenceDate">The reference date to calculate age from (defaults to today in UTC).</param>
    /// <returns>True if age is less than the limit, false otherwise.</returns>
    public static bool IsChild(DateOnly? dateOfBirth, int ageLimit = 18, DateOnly? referenceDate = null)
    {
        if (!dateOfBirth.HasValue) return false;
        return CalculateAge(dateOfBirth.Value, referenceDate) < ageLimit;
    }

    #endregion

    #region Date Navigation

    /// <summary>
    /// Gets the Monday (start of week) for a given date using ISO 8601 week definition.
    /// </summary>
    /// <param name="date">The date to find the week start for.</param>
    /// <returns>The Monday of the week containing the date.</returns>
    public static DateOnly GetWeekStart(DateOnly date)
    {
        var dayOfWeek = (int)date.DayOfWeek;
        // ISO 8601: Monday = 1, Sunday = 7
        var daysFromMonday = dayOfWeek == 0 ? 6 : dayOfWeek - 1;
        return date.AddDays(-daysFromMonday);
    }

    /// <summary>
    /// Gets the first day of the month for a given date.
    /// </summary>
    /// <param name="date">The date to find the month start for.</param>
    /// <returns>The first day of the month.</returns>
    public static DateOnly GetMonthStart(DateOnly date)
    {
        return new DateOnly(date.Year, date.Month, 1);
    }

    /// <summary>
    /// Gets the last day of the month for a given date.
    /// </summary>
    /// <param name="date">The date to find the month end for.</param>
    /// <returns>The last day of the month.</returns>
    public static DateOnly GetMonthEnd(DateOnly date)
    {
        var daysInMonth = DateTime.DaysInMonth(date.Year, date.Month);
        return new DateOnly(date.Year, date.Month, daysInMonth);
    }

    #endregion
}
