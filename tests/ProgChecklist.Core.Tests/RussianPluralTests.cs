namespace ProgChecklist.Core.Tests;

public class RussianPluralTests
{
    [Theory]
    [InlineData(0, "many")]
    [InlineData(1, "one")]
    [InlineData(2, "few")]
    [InlineData(4, "few")]
    [InlineData(5, "many")]
    [InlineData(11, "many")]
    [InlineData(12, "many")]
    [InlineData(14, "many")]
    [InlineData(21, "one")]
    [InlineData(22, "few")]
    [InlineData(25, "many")]
    [InlineData(111, "many")]
    [InlineData(254, "few")]
    [InlineData(-1, "one")]
    public void Choose_Number_ReturnsExpectedForm(int n, string expected)
    {
        // Arrange & Act
        var result = RussianPlural.Choose(n, "one", "few", "many");

        // Assert
        Assert.Equal(expected, result);
    }
}
