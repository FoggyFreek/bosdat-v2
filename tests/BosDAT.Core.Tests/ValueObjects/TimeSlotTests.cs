using BosDAT.Core.ValueObjects;
using Xunit;

namespace BosDAT.Core.Tests.ValueObjects;

public class TimeSlotTests
{
    [Fact]
    public void TimeSlot_Constructor_ShouldCreateValidInstance()
    {
        // Arrange & Act
        var timeSlot = new TimeSlot(DayOfWeek.Monday, new TimeOnly(10, 0), new TimeOnly(11, 30));

        // Assert
        Assert.Equal(DayOfWeek.Monday, timeSlot.DayOfWeek);
        Assert.Equal(new TimeOnly(10, 0), timeSlot.StartTime);
        Assert.Equal(new TimeOnly(11, 30), timeSlot.EndTime);
    }

    [Fact]
    public void TimeSlot_Constructor_ShouldThrowWhenEndTimeBeforeStartTime()
    {
        // Arrange, Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new TimeSlot(DayOfWeek.Monday, new TimeOnly(11, 30), new TimeOnly(10, 0)));

        Assert.Contains("End time must be after start time", exception.Message);
    }

    [Fact]
    public void TimeSlot_Constructor_ShouldThrowWhenEndTimeEqualsStartTime()
    {
        // Arrange, Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new TimeSlot(DayOfWeek.Monday, new TimeOnly(10, 0), new TimeOnly(10, 0)));

        Assert.Contains("End time must be after start time", exception.Message);
    }

    [Fact]
    public void OverlapsWith_DifferentDays_ShouldReturnFalse()
    {
        // Arrange
        var slot1 = new TimeSlot(DayOfWeek.Monday, new TimeOnly(10, 0), new TimeOnly(11, 30));
        var slot2 = new TimeSlot(DayOfWeek.Tuesday, new TimeOnly(10, 0), new TimeOnly(11, 30));

        // Act
        var overlaps = slot1.OverlapsWith(slot2);

        // Assert
        Assert.False(overlaps);
    }

    [Fact]
    public void OverlapsWith_SameDayNoOverlap_ShouldReturnFalse()
    {
        // Arrange
        var slot1 = new TimeSlot(DayOfWeek.Monday, new TimeOnly(10, 0), new TimeOnly(11, 30));
        var slot2 = new TimeSlot(DayOfWeek.Monday, new TimeOnly(12, 0), new TimeOnly(13, 30));

        // Act
        var overlaps = slot1.OverlapsWith(slot2);

        // Assert
        Assert.False(overlaps);
    }

    [Fact]
    public void OverlapsWith_AdjacentTimes_ShouldReturnFalse()
    {
        // Arrange
        var slot1 = new TimeSlot(DayOfWeek.Monday, new TimeOnly(10, 0), new TimeOnly(11, 30));
        var slot2 = new TimeSlot(DayOfWeek.Monday, new TimeOnly(11, 30), new TimeOnly(13, 0));

        // Act
        var overlaps = slot1.OverlapsWith(slot2);

        // Assert
        Assert.False(overlaps);
    }

    [Fact]
    public void OverlapsWith_PartialOverlap_ShouldReturnTrue()
    {
        // Arrange
        var slot1 = new TimeSlot(DayOfWeek.Monday, new TimeOnly(10, 0), new TimeOnly(11, 30));
        var slot2 = new TimeSlot(DayOfWeek.Monday, new TimeOnly(11, 0), new TimeOnly(12, 0));

        // Act
        var overlaps = slot1.OverlapsWith(slot2);

        // Assert
        Assert.True(overlaps);
    }

    [Fact]
    public void OverlapsWith_CompleteOverlap_ShouldReturnTrue()
    {
        // Arrange
        var slot1 = new TimeSlot(DayOfWeek.Monday, new TimeOnly(10, 0), new TimeOnly(14, 0));
        var slot2 = new TimeSlot(DayOfWeek.Monday, new TimeOnly(11, 0), new TimeOnly(12, 0));

        // Act
        var overlaps = slot1.OverlapsWith(slot2);

        // Assert
        Assert.True(overlaps);
    }

    [Fact]
    public void OverlapsWith_ExactSameTime_ShouldReturnTrue()
    {
        // Arrange
        var slot1 = new TimeSlot(DayOfWeek.Monday, new TimeOnly(10, 0), new TimeOnly(11, 30));
        var slot2 = new TimeSlot(DayOfWeek.Monday, new TimeOnly(10, 0), new TimeOnly(11, 30));

        // Act
        var overlaps = slot1.OverlapsWith(slot2);

        // Assert
        Assert.True(overlaps);
    }

    [Fact]
    public void OverlapsWith_ContainedSlot_ShouldReturnTrue()
    {
        // Arrange
        var slot1 = new TimeSlot(DayOfWeek.Monday, new TimeOnly(11, 0), new TimeOnly(12, 0));
        var slot2 = new TimeSlot(DayOfWeek.Monday, new TimeOnly(10, 0), new TimeOnly(14, 0));

        // Act
        var overlaps = slot1.OverlapsWith(slot2);

        // Assert
        Assert.True(overlaps);
    }

    [Fact]
    public void OverlapsWith_IsSymmetric()
    {
        // Arrange
        var slot1 = new TimeSlot(DayOfWeek.Monday, new TimeOnly(10, 0), new TimeOnly(11, 30));
        var slot2 = new TimeSlot(DayOfWeek.Monday, new TimeOnly(11, 0), new TimeOnly(12, 0));

        // Act
        var overlaps1 = slot1.OverlapsWith(slot2);
        var overlaps2 = slot2.OverlapsWith(slot1);

        // Assert
        Assert.Equal(overlaps1, overlaps2);
    }
}
