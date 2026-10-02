namespace ProgChecklist.Core.Tests;

public class LevelExtensionsTests
{
    [Theory]
    [InlineData(Level.Junior, "Junior")]
    [InlineData(Level.Middle, "Middle")]
    [InlineData(Level.Senior, "Senior")]
    public void ToDisplayName_KnownLevel_ReturnsName(Level level, string expected)
    {
        // Arrange & Act
        var result = level.ToDisplayName();

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(Level.Junior, "junior")]
    [InlineData(Level.Middle, "middle")]
    [InlineData(Level.Senior, "senior")]
    public void ToCode_KnownLevel_ReturnsLowercaseCode(Level level, string expected)
    {
        // Arrange & Act
        var result = level.ToCode();

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToDisplayName_UnknownLevel_Throws()
    {
        // Arrange
        var level = (Level)42;

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => level.ToDisplayName());
    }

    [Fact]
    public void ToCode_UnknownLevel_Throws()
    {
        // Arrange
        var level = (Level)42;

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => level.ToCode());
    }
}
