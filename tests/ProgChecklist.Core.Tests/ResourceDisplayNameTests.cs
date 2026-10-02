namespace ProgChecklist.Core.Tests;

public class ResourceDisplayNameTests
{
    public static TheoryData<ResourceKind> AllKinds() => new(Enum.GetValues<ResourceKind>());

    public static TheoryData<ResourceLanguage> AllLanguages() => new(Enum.GetValues<ResourceLanguage>());

    [Theory]
    [MemberData(nameof(AllKinds))]
    public void ToDisplayName_EveryKind_ReturnsNonEmptyLabel(ResourceKind kind)
    {
        // Arrange & Act
        var result = kind.ToDisplayName();

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(result));
    }

    [Fact]
    public void ToDisplayName_Kinds_ReturnDistinctExpectedLabels()
    {
        // Arrange
        var kinds = Enum.GetValues<ResourceKind>();

        // Act
        var labels = kinds.Select(k => k.ToDisplayName()).ToList();

        // Assert
        Assert.Equal(kinds.Length, labels.Distinct().Count());
        Assert.Equal("Документация", ResourceKind.Docs.ToDisplayName());
        Assert.Equal("Статья", ResourceKind.Article.ToDisplayName());
        Assert.Equal("Видео", ResourceKind.Video.ToDisplayName());
        Assert.Equal("Курс", ResourceKind.Course.ToDisplayName());
        Assert.Equal("Книга", ResourceKind.Book.ToDisplayName());
        Assert.Equal("Практика", ResourceKind.Practice.ToDisplayName());
    }

    [Theory]
    [MemberData(nameof(AllLanguages))]
    public void ToDisplayName_EveryLanguage_ReturnsNonEmptyLabel(ResourceLanguage language)
    {
        // Arrange & Act
        var result = language.ToDisplayName();

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(result));
    }

    [Fact]
    public void ToDisplayName_Languages_ReturnExpectedLabels()
    {
        // Arrange & Act & Assert
        Assert.Equal("на русском", ResourceLanguage.Russian.ToDisplayName());
        Assert.Equal("на английском", ResourceLanguage.English.ToDisplayName());
    }

    [Fact]
    public void ToDisplayName_UnknownKind_Throws()
    {
        // Arrange
        var kind = (ResourceKind)42;

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => kind.ToDisplayName());
    }

    [Fact]
    public void ToDisplayName_UnknownLanguage_Throws()
    {
        // Arrange
        var language = (ResourceLanguage)42;

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => language.ToDisplayName());
    }
}
