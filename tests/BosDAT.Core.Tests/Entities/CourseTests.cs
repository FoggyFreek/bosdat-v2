using BosDAT.Core.Entities;
using BosDAT.Core.Enums;
using Xunit;

namespace BosDAT.Core.Tests.Entities;

public class CourseTests
{
    [Fact]
    public void Course_DefaultWeekParity_ShouldBeAll()
    {
        // Arrange & Act
        var course = new Course();

        // Assert
        Assert.Equal(WeekParity.All, course.WeekParity);
    }

    [Fact]
    public void Course_CanSetWeekParityToOdd()
    {
        // Arrange
        var course = new Course();

        // Act
        course.WeekParity = WeekParity.Odd;

        // Assert
        Assert.Equal(WeekParity.Odd, course.WeekParity);
    }

    [Fact]
    public void Course_CanSetWeekParityToEven()
    {
        // Arrange
        var course = new Course();

        // Act
        course.WeekParity = WeekParity.Even;

        // Assert
        Assert.Equal(WeekParity.Even, course.WeekParity);
    }

    [Fact]
    public void Course_WeeklyFrequency_CanHaveAnyWeekParity()
    {
        // Arrange
        var course = new Course
        {
            Frequency = CourseFrequency.Weekly
        };

        // Act & Assert - No entity-level validation, all values allowed
        course.WeekParity = WeekParity.All;
        Assert.Equal(WeekParity.All, course.WeekParity);

        course.WeekParity = WeekParity.Odd;
        Assert.Equal(WeekParity.Odd, course.WeekParity);

        course.WeekParity = WeekParity.Even;
        Assert.Equal(WeekParity.Even, course.WeekParity);
    }

    [Fact]
    public void Course_BiweeklyFrequency_CanHaveAnyWeekParity()
    {
        // Arrange
        var course = new Course
        {
            Frequency = CourseFrequency.Biweekly
        };

        // Act & Assert - No entity-level validation, all values allowed
        course.WeekParity = WeekParity.All;
        Assert.Equal(WeekParity.All, course.WeekParity);

        course.WeekParity = WeekParity.Odd;
        Assert.Equal(WeekParity.Odd, course.WeekParity);

        course.WeekParity = WeekParity.Even;
        Assert.Equal(WeekParity.Even, course.WeekParity);
    }
}
