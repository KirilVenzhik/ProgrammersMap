namespace ProgChecklist.Core.Tests;

public class SeoTextTests
{
    private static JsonTopicCatalog LoadReal() =>
        JsonTopicCatalog.LoadFromFile(Path.Combine(AppContext.BaseDirectory, "content", "topics.json"));

    [Fact]
    public void HomeDescription_RealCatalog_IsNonEmptyWithinLimitAndHasCounts()
    {
        // Arrange
        var catalog = LoadReal();

        // Act
        var text = SeoText.HomeDescription(catalog);

        // Assert
        Assert.InRange(text.Length, 1, 160);
        Assert.Contains("254", text);
        Assert.Contains("14", text);
    }

    [Fact]
    public void SectionDescription_EverySection_IsWithinLimitAndContainsTitle()
    {
        // Arrange
        var catalog = LoadReal();

        // Act & Assert
        foreach (var section in catalog.GetSections())
        {
            var text = SeoText.SectionDescription(section);
            Assert.InRange(text.Length, 1, 160);
            Assert.Contains(section.Title, text);
        }
    }

    [Fact]
    public void TopicDescription_EveryTopic_IsWithinLimitAndContainsTitle()
    {
        // Arrange
        var catalog = LoadReal();

        // Act & Assert
        foreach (var section in catalog.GetSections())
        {
            foreach (var topic in section.Groups.SelectMany(group => group.Topics))
            {
                var context = catalog.FindTopicContext(section.Slug, topic.Slug)!;
                var text = SeoText.TopicDescription(context);
                Assert.InRange(text.Length, 1, 160);
                Assert.Contains(topic.Title, text);
            }
        }
    }

    [Fact]
    public void TopicDescription_EveryTopic_EndsWithPeriodOrEllipsisNotMidSentence()
    {
        // Arrange
        var catalog = LoadReal();

        // Act & Assert
        foreach (var section in catalog.GetSections())
        {
            foreach (var topic in section.Groups.SelectMany(group => group.Topics))
            {
                var text = SeoText.TopicDescription(catalog.FindTopicContext(section.Slug, topic.Slug)!);
                Assert.True(text.EndsWith('.') || text.EndsWith('…'), text);
                Assert.DoesNotContain("Карте…", text);
            }
        }
    }

    [Fact]
    public void HomeDescription_RealCatalog_UsesCorrectPlurals()
    {
        // Arrange
        var catalog = LoadReal();

        // Act
        var text = SeoText.HomeDescription(catalog);

        // Assert
        Assert.Contains("254 темы в 14 разделах", text);
    }

    [Fact]
    public void SectionDescription_SingleLevelSection_ListsOnlyThatLevel()
    {
        // Arrange
        const string json = """
            { "sections": [
              { "slug": "a", "title": "A", "order": 1, "groups": [
                { "slug": "g", "title": "G", "topics": [ { "slug": "t", "title": "T", "level": "senior" } ] } ] } ] }
            """;
        var section = JsonTopicCatalog.Parse(json).GetSections()[0];

        // Act
        var text = SeoText.SectionDescription(section);

        // Assert
        Assert.Contains("1 тема", text);
        Assert.EndsWith("Уровень Senior.", text);
    }

    [Fact]
    public void SectionDescription_TwoLevelSection_ListsTwoLevelsInOrder()
    {
        // Arrange
        const string json = """
            { "sections": [
              { "slug": "a", "title": "A", "order": 1, "groups": [
                { "slug": "g", "title": "G", "topics": [
                  { "slug": "t", "title": "T", "level": "middle" },
                  { "slug": "u", "title": "U", "level": "junior" } ] } ] } ] }
            """;
        var section = JsonTopicCatalog.Parse(json).GetSections()[0];

        // Act
        var text = SeoText.SectionDescription(section);

        // Assert
        Assert.EndsWith("Уровни Junior и Middle.", text);
    }

    [Fact]
    public void SectionDescription_RealCatalog_LevelsMatchSectionContent()
    {
        // Arrange
        var catalog = LoadReal();

        // Act & Assert
        foreach (var section in catalog.GetSections())
        {
            var text = SeoText.SectionDescription(section);
            var levels = section.Groups.SelectMany(group => group.Topics).Select(topic => topic.Level).Distinct().ToList();
            foreach (var level in Enum.GetValues<Level>())
            {
                Assert.Equal(levels.Contains(level), text.Contains(level.ToDisplayName()));
            }
        }
    }

    private static TopicContext BuildContext(string topicTitle)
    {
        var json = $$"""
            { "sections": [ { "slug": "a", "title": "A", "order": 1, "groups": [
              { "slug": "g", "title": "G", "topics": [ { "slug": "t", "title": "{{topicTitle}}", "level": "junior" } ] } ] } ] }
            """;
        return JsonTopicCatalog.Parse(json).FindTopicContext("a", "t")!;
    }

    private static Lesson LessonWith(string summary) => new("a", "t", "<p>x</p>", summary);

    [Fact]
    public void TopicDescription_ShortFirstSentence_AppendsClosingSentence()
    {
        // Arrange
        var context = BuildContext("Короткая тема");

        // Act
        var text = SeoText.TopicDescription(context);

        // Assert
        Assert.EndsWith(" Отмечайте изученное на Карте программиста.", text);
    }

    [Fact]
    public void TopicDescription_FirstSentenceAlone_DropsClosingSentenceWhenOverflowing()
    {
        // Arrange
        var context = BuildContext(string.Join(' ', Enumerable.Repeat("длинное", 12)));

        // Act
        var text = SeoText.TopicDescription(context);

        // Assert
        Assert.DoesNotContain("Отмечайте изученное", text);
        Assert.InRange(text.Length, 1, 160);
    }

    [Fact]
    public void TopicDescriptionWithLesson_ShortSummary_ReturnsSummaryAsIs()
    {
        // Arrange
        var context = BuildContext("T");

        // Act
        var text = SeoText.TopicDescription(context, LessonWith("Короткое резюме урока."));

        // Assert
        Assert.Equal("Короткое резюме урока.", text);
    }

    [Fact]
    public void TopicDescriptionWithLesson_LongSummary_TruncatesAtWordBoundary()
    {
        // Arrange
        var context = BuildContext("T");
        var summary = string.Join(' ', Enumerable.Repeat("слово", 60));

        // Act
        var text = SeoText.TopicDescription(context, LessonWith(summary));

        // Assert
        Assert.True(text.Length <= 160);
        Assert.EndsWith("…", text);
        Assert.All(text[..^1].Split(' '), word => Assert.Equal("слово", word));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void TopicDescriptionWithLesson_BlankSummary_FallsBackToCatalogDescription(string summary)
    {
        // Arrange
        var context = BuildContext("T");

        // Act
        var text = SeoText.TopicDescription(context, LessonWith(summary));

        // Assert
        Assert.Equal(SeoText.TopicDescription(context), text);
    }

    [Fact]
    public void TopicDescriptionWithLesson_NullLesson_FallsBackToCatalogDescription()
    {
        // Arrange
        var context = BuildContext("T");

        // Act
        var text = SeoText.TopicDescription(context, null);

        // Assert
        Assert.Equal(SeoText.TopicDescription(context), text);
    }

    [Fact]
    public void Truncate_ShortText_ReturnsUnchanged()
    {
        // Arrange & Act
        var result = SeoText.Truncate("short text", 160);

        // Assert
        Assert.Equal("short text", result);
    }

    [Fact]
    public void Truncate_LongText_CutsAtWordBoundaryAndAppendsEllipsis()
    {
        // Arrange
        var text = "word1 word2 word3 word4 word5 word6";

        // Act
        var result = SeoText.Truncate(text, 20);

        // Assert
        Assert.Equal("word1 word2…", result);
        Assert.True(result.Length <= 20);
    }

    [Fact]
    public void Truncate_NoWordBoundary_HardCuts()
    {
        // Arrange
        var text = new string('a', 50);

        // Act
        var result = SeoText.Truncate(text, 20);

        // Assert
        Assert.Equal(new string('a', 17) + "…", result);
    }
}
