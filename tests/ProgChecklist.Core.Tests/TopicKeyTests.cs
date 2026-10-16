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

    private static readonly JsonTopicCatalog Catalog =
        JsonTopicCatalog.LoadFromFile(Path.Combine(AppContext.BaseDirectory, "content", "topics.json"));

    [Fact]
    public void TryParse_ValidKey_ReturnsPartsAndTrue()
    {
        // Arrange & Act
        var ok = TopicKey.TryParse("git/init-add-commit", out var section, out var topic);

        // Assert
        Assert.True(ok);
        Assert.Equal("git", section);
        Assert.Equal("init-add-commit", topic);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("git")]
    [InlineData("a/b/c")]
    [InlineData("/b")]
    [InlineData("a/")]
    [InlineData("/")]
    [InlineData(" git/init-add-commit")]
    [InlineData("git/init-add-commit ")]
    [InlineData("git/init add")]
    [InlineData("git\t/x")]
    public void TryParse_MalformedKey_ReturnsFalse(string? key)
    {
        // Arrange & Act
        var ok = TopicKey.TryParse(key, out _, out _);

        // Assert
        Assert.False(ok);
    }

    [Theory]
    [InlineData("git/init-add-commit")]
    [InlineData("databases/join")]
    [InlineData("fundamentals/binary-bits-bytes")]
    public void IsKnown_ExistingTopic_ReturnsTrue(string key)
    {
        // Arrange & Act & Assert
        Assert.True(TopicKey.IsKnown(Catalog, key));
    }

    [Theory]
    [InlineData("git/no-such-topic")]
    [InlineData("nosuch/init-add-commit")]
    [InlineData("git/init-add-commit/x")]
    [InlineData(" git/init-add-commit")]
    [InlineData("Git/init-add-commit")]
    [InlineData("git/Init-Add-Commit")]
    [InlineData("")]
    [InlineData(null)]
    public void IsKnown_UnknownOrMalformedOrWrongCase_ReturnsFalse(string? key)
    {
        // Arrange & Act & Assert
        Assert.False(TopicKey.IsKnown(Catalog, key));
    }

    [Fact]
    public void Normalize_MixedInput_KeepsKnownUniqueInFirstOccurrenceOrder()
    {
        // Arrange
        string?[] input =
        [
            "git/init-add-commit", "bad", null, "databases/join", "git/init-add-commit",
            "Git/init-add-commit", "fundamentals/binary-bits-bytes", "databases/join",
        ];

        // Act
        var result = TopicKey.Normalize(Catalog, input);

        // Assert
        Assert.Equal(new[] { "git/init-add-commit", "databases/join", "fundamentals/binary-bits-bytes" }, result);
    }
}
