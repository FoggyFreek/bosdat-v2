namespace BosDAT.Core.Enums;

/// <summary>
/// Represents the ISO week parity for biweekly courses.
/// </summary>
public enum WeekParity
{
    /// <summary>
    /// Weekly courses (every week) or no parity restriction.
    /// </summary>
    All = 0,

    /// <summary>
    /// Odd ISO weeks (1, 3, 5, 7, etc.).
    /// </summary>
    Odd = 1,

    /// <summary>
    /// Even ISO weeks (2, 4, 6, 8, etc.).
    /// </summary>
    Even = 2
}
