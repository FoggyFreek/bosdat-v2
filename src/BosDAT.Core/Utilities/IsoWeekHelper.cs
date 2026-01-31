using System.Globalization;
using BosDAT.Core.Enums;

namespace BosDAT.Core.Utilities;

/// <summary>
/// Utility class for ISO 8601 week-based date calculations.
/// </summary>
public static class IsoWeekHelper
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
        var dec31 = new DateTime(isoYear, 12, 31);
        var isoWeekOfDec31 = GetIsoWeekNumber(dec31);
        var isoYearOfDec31 = GetIsoWeekYear(dec31);

        // If Dec 31 is in week 53 of the same ISO year, it's a 53-week year
        if (isoYearOfDec31 == isoYear && isoWeekOfDec31 == 53)
        {
            return true;
        }

        // Check a few days before Dec 31 (since Dec 31 might be in Week 1 of next year)
        var dec28 = new DateTime(isoYear, 12, 28);
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
}
