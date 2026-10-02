namespace ProgChecklist.Core.Tests;

public class JsonTopicCatalogTests
{
    private static readonly string RealFilePath =
        Path.Combine(AppContext.BaseDirectory, "content", "topics.json");

    private static JsonTopicCatalog LoadReal() => JsonTopicCatalog.LoadFromFile(RealFilePath);

    [Fact]
    public void LoadFromFile_RealFile_Has14Sections()
    {
        // Arrange & Act
        var catalog = LoadReal();

        // Assert
        Assert.Equal(14, catalog.GetSections().Count);
    }

    [Fact]
    public void TopicCount_RealFile_Is254()
    {
        // Arrange & Act
        var catalog = LoadReal();

        // Assert
        Assert.Equal(254, catalog.TopicCount);
    }

    [Fact]
    public void LoadFromFile_RealFile_Has254Topics()
    {
        // Arrange & Act
        var catalog = LoadReal();

        // Assert
        Assert.Equal(254, catalog.GetSections().Sum(section => section.TopicCount));
    }

    [Fact]
    public void GetSections_RealFile_SortedByOrderWithExpectedFirst()
    {
        // Arrange
        var catalog = LoadReal();

        // Act
        var sections = catalog.GetSections();

        // Assert
        Assert.Equal(
            sections.OrderBy(section => section.Order).Select(section => section.Slug),
            sections.Select(section => section.Slug));
        Assert.Equal("fundamentals", sections[0].Slug);
        Assert.Equal("Основы программирования", sections[0].Title);
    }

    [Fact]
    public void FindSection_ExistingSlug_ReturnsSection()
    {
        // Arrange
        var catalog = LoadReal();

        // Act
        var section = catalog.FindSection("fundamentals");

        // Assert
        Assert.NotNull(section);
    }

    [Fact]
    public void FindSection_DifferentCase_ReturnsSection()
    {
        // Arrange
        var catalog = LoadReal();

        // Act
        var section = catalog.FindSection("FUNDAMENTALS");

        // Assert
        Assert.NotNull(section);
    }

    [Fact]
    public void FindSection_UnknownSlug_ReturnsNull()
    {
        // Arrange
        var catalog = LoadReal();

        // Act
        var section = catalog.FindSection("no-such-section");

        // Assert
        Assert.Null(section);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void FindSection_EmptyOrWhitespace_ReturnsNull(string slug)
    {
        // Arrange
        var catalog = LoadReal();

        // Act
        var section = catalog.FindSection(slug);

        // Assert
        Assert.Null(section);
    }

    [Fact]
    public void FindTopic_ExistingPair_ReturnsTopic()
    {
        // Arrange
        var catalog = LoadReal();

        // Act
        var topic = catalog.FindTopic("fundamentals", "binary-bits-bytes");

        // Assert
        Assert.NotNull(topic);
        Assert.Equal("Двоичная система, биты и байты", topic.Title);
        Assert.Equal(Level.Junior, topic.Level);
    }

    [Fact]
    public void FindTopic_TopicInOtherSection_ReturnsNull()
    {
        // Arrange
        var catalog = LoadReal();
        var otherSection = catalog.GetSections()[1].Slug;

        // Act
        var topic = catalog.FindTopic(otherSection, "binary-bits-bytes");

        // Assert
        Assert.Null(topic);
    }

    [Fact]
    public void FindTopic_UnknownSlugs_ReturnsNull()
    {
        // Arrange
        var catalog = LoadReal();

        // Act
        var unknownTopic = catalog.FindTopic("fundamentals", "nope");
        var unknownSection = catalog.FindTopic("nope", "nope");
        var empty = catalog.FindTopic("", "");

        // Assert
        Assert.Null(unknownTopic);
        Assert.Null(unknownSection);
        Assert.Null(empty);
    }

    [Fact]
    public void Slugs_RealFile_MatchFormat()
    {
        // Arrange
        var pattern = new System.Text.RegularExpressions.Regex("^[a-z0-9]+(-[a-z0-9]+)*$");
        var offenders = new List<string>();

        void Check(string path, string slug)
        {
            if (slug.Length > 40 || !pattern.IsMatch(slug))
            {
                offenders.Add($"{path}: '{slug}'");
            }
        }

        // Act
        foreach (var section in LoadReal().GetSections())
        {
            Check("section", section.Slug);
            foreach (var group in section.Groups)
            {
                Check($"group in {section.Slug}", group.Slug);
                foreach (var topic in group.Topics)
                {
                    Check($"topic in {section.Slug}", topic.Slug);
                }
            }
        }

        // Assert
        Assert.True(offenders.Count == 0, "Invalid slugs: " + string.Join("; ", offenders));
    }

    [Fact]
    public void TopicKeys_RealFile_AreUnique()
    {
        // Arrange
        var keys = LoadReal().GetSections()
            .SelectMany(section => section.Groups.SelectMany(group => group.Topics),
                (section, topic) => $"{section.Slug}/{topic.Slug}")
            .ToList();

        // Act
        var duplicates = keys.GroupBy(key => key).Where(g => g.Count() > 1).Select(g => g.Key).ToList();

        // Assert
        Assert.Equal(254, keys.Count);
        Assert.True(duplicates.Count == 0, "Duplicate keys: " + string.Join("; ", duplicates));
    }

    [Fact]
    public void Parse_DuplicateTopicInSameGroup_Throws()
    {
        // Arrange
        const string json = """
            { "sections": [ { "slug": "s", "title": "S", "order": 1, "groups": [
              { "slug": "g", "title": "G", "topics": [
                { "slug": "t", "title": "T1", "level": "junior" },
                { "slug": "T", "title": "T2", "level": "junior" } ] } ] } ] }
            """;

        // Act
        var exception = Assert.Throws<TopicCatalogException>(() => JsonTopicCatalog.Parse(json));

        // Assert
        Assert.Contains("s/T", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_DuplicateTopicAcrossGroups_Throws()
    {
        // Arrange
        const string json = """
            { "sections": [ { "slug": "s", "title": "S", "order": 1, "groups": [
              { "slug": "g1", "title": "G1", "topics": [ { "slug": "t", "title": "T1", "level": "junior" } ] },
              { "slug": "g2", "title": "G2", "topics": [ { "slug": "t", "title": "T2", "level": "middle" } ] } ] } ] }
            """;

        // Act & Assert
        Assert.Throws<TopicCatalogException>(() => JsonTopicCatalog.Parse(json));
    }

    [Fact]
    public void Parse_SameTopicSlugInDifferentSections_Succeeds()
    {
        // Arrange
        const string json = """
            { "sections": [
              { "slug": "a", "title": "A", "order": 1, "groups": [
                { "slug": "g", "title": "G", "topics": [ { "slug": "t", "title": "T", "level": "junior" } ] } ] },
              { "slug": "b", "title": "B", "order": 2, "groups": [
                { "slug": "g", "title": "G", "topics": [ { "slug": "t", "title": "T", "level": "senior" } ] } ] } ] }
            """;

        // Act
        var catalog = JsonTopicCatalog.Parse(json);

        // Assert
        Assert.Equal(Level.Junior, catalog.FindTopic("a", "t")!.Level);
        Assert.Equal(Level.Senior, catalog.FindTopic("b", "t")!.Level);
    }

    [Fact]
    public void Parse_DuplicateSectionSlug_Throws()
    {
        // Arrange
        const string json = """
            { "sections": [
              { "slug": "a", "title": "A", "order": 1, "groups": [] },
              { "slug": "A", "title": "A2", "order": 2, "groups": [] } ] }
            """;

        // Act & Assert
        Assert.Throws<TopicCatalogException>(() => JsonTopicCatalog.Parse(json));
    }

    [Fact]
    public void Parse_UnknownLevel_Throws()
    {
        // Arrange
        const string json = """
            { "sections": [ { "slug": "s", "title": "S", "order": 1, "groups": [
              { "slug": "g", "title": "G", "topics": [ { "slug": "t", "title": "T", "level": "expert" } ] } ] } ] }
            """;

        // Act & Assert
        Assert.Throws<TopicCatalogException>(() => JsonTopicCatalog.Parse(json));
    }

    [Fact]
    public void Parse_InvalidJson_Throws()
    {
        // Arrange
        const string json = "{ not json";

        // Act & Assert
        Assert.Throws<TopicCatalogException>(() => JsonTopicCatalog.Parse(json));
    }

    [Fact]
    public void LoadFromFile_MissingFile_ThrowsWithPath()
    {
        // Arrange
        var path = Path.Combine(AppContext.BaseDirectory, "missing", "topics.json");

        // Act
        var exception = Assert.Throws<TopicCatalogException>(() => JsonTopicCatalog.LoadFromFile(path));

        // Assert
        Assert.Contains(path, exception.Message);
    }

    [Fact]
    public void TopicCount_MultipleGroups_SumsTopics()
    {
        // Arrange
        const string json = """
            { "sections": [ { "slug": "s", "title": "S", "order": 1, "groups": [
              { "slug": "g1", "title": "G1", "topics": [
                { "slug": "t1", "title": "T1", "level": "junior" },
                { "slug": "t2", "title": "T2", "level": "middle" } ] },
              { "slug": "g2", "title": "G2", "topics": [ { "slug": "t3", "title": "T3", "level": "senior" } ] } ] } ] }
            """;

        // Act
        var section = JsonTopicCatalog.Parse(json).FindSection("s");

        // Assert
        Assert.Equal(3, section!.TopicCount);
    }
}
