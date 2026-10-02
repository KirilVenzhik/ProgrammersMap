namespace ProgChecklist.Core.Tests;

public class TopicKeyTests
{
    [Fact]
    public void Create_ValidSlugs_ReturnsSectionSlashTopic()
    {
        // Arrange & Act
        var key = TopicKey.Create("basics", "variables");

        // Assert
        Assert.Equal("basics/variables", key);
    }

    [Theory]
    [InlineData(null, "t")]
    [InlineData("", "t")]
    [InlineData("  ", "t")]
    [InlineData("s", null)]
    [InlineData("s", "")]
    [InlineData("s", " ")]
    public void Create_NullOrBlankArgument_Throws(string? section, string? topic)
    {
        // Arrange & Act & Assert
        Assert.ThrowsAny<ArgumentException>(() => TopicKey.Create(section!, topic!));
    }
}
