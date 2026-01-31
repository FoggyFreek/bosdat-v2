using BosDAT.Core.Enums;
using Xunit;

namespace BosDAT.Core.Tests.Enums;

public class WeekParityTests
{
    [Fact]
    public void WeekParity_ShouldHaveAllValue()
    {
        // Arrange & Act
        var all = WeekParity.All;

        // Assert
        Assert.Equal(0, (int)all);
    }

    [Fact]
    public void WeekParity_ShouldHaveOddValue()
    {
        // Arrange & Act
        var odd = WeekParity.Odd;

        // Assert
        Assert.Equal(1, (int)odd);
    }

    [Fact]
    public void WeekParity_ShouldHaveEvenValue()
    {
        // Arrange & Act
        var even = WeekParity.Even;

        // Assert
        Assert.Equal(2, (int)even);
    }

    [Fact]
    public void WeekParity_ShouldHaveCorrectEnumValues()
    {
        // Arrange & Act
        var values = Enum.GetValues<WeekParity>();

        // Assert
        Assert.Equal(3, values.Length);
        Assert.Contains(WeekParity.All, values);
        Assert.Contains(WeekParity.Odd, values);
        Assert.Contains(WeekParity.Even, values);
    }
}
