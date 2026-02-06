using System.Globalization;
using BosDAT.Core.Enums;

namespace BosDAT.Core.Utilities;

/// <summary>
/// Utility class for ISO 8601 date/time operations including week-based calculations,
/// date conversions, and local datetime string formatting.
/// All dates/times use local time — no UTC conversions.
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
    /// Uses the native .NET ISOWeek.GetWeeksInYear method.
    /// </summary>
    /// <param name="isoYear">The ISO year to check.</param>
    /// <returns>True if the year has 53 weeks, false otherwise.</returns>
    public static bool Is53WeekYear(int isoYear)
    {
        return ISOWeek.GetWeeksInYear(isoYear) == 53;
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
    /// Converts a DateOnly to DateTime at midnight (local).
    /// </summary>
    /// <param name="date">The date to convert.</param>
    /// <returns>DateTime at 00:00:00.</returns>
    public static DateTime ToDateTime(DateOnly date)
    {
        return date.ToDateTime(TimeOnly.MinValue);
    }

    /// <summary>
    /// Converts a DateOnly and TimeOnly to DateTime (local).
    /// </summary>
    /// <param name="date">The date component.</param>
    /// <param name="time">The time component.</param>
    /// <returns>DateTime with the specified date and time.</returns>
    public static DateTime ToDateTime(DateOnly date, TimeOnly time)
    {
        return date.ToDateTime(time);
    }

    /// <summary>
    /// Gets the current date as DateOnly in local time.
    /// </summary>
    /// <returns>Today's date as DateOnly.</returns>
    public static DateOnly Today()
    {
        return DateOnly.FromDateTime(DateTime.Now);
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

    #region Local DateTime String Formatting

    /// <summary>
    /// Creates a local datetime string from date and time components.
    /// Supports time formats: 'HH:mm:ss', 'HH:mm:ss:ff' (with fractional seconds treated as milliseconds).
    /// </summary>
    /// <param name="date">The date in MM-dd-yyyy format (e.g., "02-15-2026").</param>
    /// <param name="time">The time in HH:mm:ss or HH:mm:ss:ff format (e.g., "19:30:00" or "19:30:00:00").</param>
    /// <returns>Local datetime string (e.g., "2026-02-15T19:30:00").</returns>
    /// <exception cref="FormatException">Thrown when date or time format is invalid.</exception>
    public static string CreateLocalDateTime(string date, string time)
    {
        var combinedFormats = new[]
        {
            "MM-dd-yyyyTHH:mm:ss",
            "MM-dd-yyyyTHH:mm:ss:ff"
        };

        var combined = $"{date}T{time}";

        if (!DateTime.TryParseExact(combined, combinedFormats, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var parsedDateTime))
        {
            throw new FormatException(
                $"Invalid date/time format: '{date}' and '{time}'. " +
                $"Expected formats: MM-dd-yyyy and HH:mm:ss or HH:mm:ss:ff");
        }

        return parsedDateTime.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Creates a local datetime string from DateOnly and time string.
    /// </summary>
    /// <param name="date">The date component.</param>
    /// <param name="time">The time in HH:mm:ss or HH:mm:ss:ff format.</param>
    /// <returns>Local datetime string.</returns>
    public static string CreateLocalDateTime(DateOnly date, string time)
    {
        var dateString = date.ToString("MM-dd-yyyy", CultureInfo.InvariantCulture);
        return CreateLocalDateTime(dateString, time);
    }

    /// <summary>
    /// Creates a local datetime string from DateOnly and TimeOnly.
    /// </summary>
    /// <param name="date">The date component.</param>
    /// <param name="time">The time component.</param>
    /// <returns>Local datetime string.</returns>
    public static string CreateLocalDateTime(DateOnly date, TimeOnly time)
    {
        var dateTime = date.ToDateTime(time);
        return dateTime.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
    }

    #endregion

    #region Age Calculation

    /// <summary>
    /// Calculates age in years from a date of birth.
    /// </summary>
    /// <param name="dateOfBirth">The date of birth.</param>
    /// <param name="referenceDate">The reference date to calculate age from (defaults to today).</param>
    /// <returns>Age in completed years.</returns>
    public static int CalculateAge(DateOnly dateOfBirth, DateOnly? referenceDate = null)
    {
        var reference = referenceDate ?? Today();
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
    /// <param name="referenceDate">The reference date to calculate age from (defaults to today).</param>
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
    /// Uses the native .NET ISOWeek.ToDateTime method.
    /// </summary>
    /// <param name="date">The date to find the week start for.</param>
    /// <returns>The Monday of the week containing the date.</returns>
    public static DateOnly GetWeekStart(DateOnly date)
    {
        var dateTime = date.ToDateTime(TimeOnly.MinValue);
        var isoYear = ISOWeek.GetYear(dateTime);
        var isoWeek = ISOWeek.GetWeekOfYear(dateTime);
        var monday = ISOWeek.ToDateTime(isoYear, isoWeek, DayOfWeek.Monday);
        return DateOnly.FromDateTime(monday);
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
