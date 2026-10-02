namespace ProgChecklist.Core.Tests;

public class SitemapPathsTests
{
    private static JsonTopicCatalog LoadReal() =>
        JsonTopicCatalog.LoadFromFile(Path.Combine(AppContext.BaseDirectory, "content", "topics.json"));

    [Fact]
    public void All_RealCatalog_Returns269Paths()
    {
        // Arrange
        var catalog = LoadReal();

        // Act
        var paths = SitemapPaths.All(catalog);

        // Assert
        Assert.Equal(1 + 14 + 254, paths.Count);
    }

    [Fact]
    public void All_RealCatalog_StartsWithHomeAndPathsAreDistinctAndRooted()
    {
        // Arrange
        var catalog = LoadReal();

        // Act
        var paths = SitemapPaths.All(catalog);

        // Assert
        Assert.Equal("/", paths[0]);
        Assert.Equal(paths.Count, paths.Distinct(StringComparer.Ordinal).Count());
        Assert.All(paths, path => Assert.StartsWith("/", path));
    }
}
