namespace BosDAT.Core.ValueObjects;

/// <summary>
/// Represents a time slot on a specific day of the week.
/// </summary>
public record TimeSlot
{
    public DayOfWeek DayOfWeek { get; init; }
    public TimeOnly StartTime { get; init; }
    public TimeOnly EndTime { get; init; }

    public TimeSlot(DayOfWeek dayOfWeek, TimeOnly startTime, TimeOnly endTime)
    {
        if (endTime <= startTime)
        {
            throw new ArgumentException("End time must be after start time.");
        }

        DayOfWeek = dayOfWeek;
        StartTime = startTime;
        EndTime = endTime;
    }

    /// <summary>
    /// Determines if this time slot overlaps with another time slot.
    /// </summary>
    /// <param name="other">The other time slot to check against.</param>
    /// <returns>True if the time slots overlap, false otherwise.</returns>
    public bool OverlapsWith(TimeSlot other)
    {
        // Different days never overlap
        if (DayOfWeek != other.DayOfWeek)
        {
            return false;
        }

        // Check for time overlap using standard interval intersection logic
        // Two intervals [a1, a2) and [b1, b2) overlap if: a1 < b2 AND a2 > b1
        return StartTime < other.EndTime && EndTime > other.StartTime;
    }
}
