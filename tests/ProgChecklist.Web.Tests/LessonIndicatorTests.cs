using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ProgChecklist.Web.Tests;

public class LessonIndicatorTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string BitsSummary = "Бит — это \"минимальная\" единица & основа данных.";
    private const string GitSummary = "Коммит фиксирует снимок проекта.";

    private readonly WebApplicationFactory<Program> _factory;

    public LessonIndicatorTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private static TempLessons CreateLessons() => new(
        ("fundamentals/binary-bits-bytes.md", $"> Вложенная цитата не должна попасть в описание.\n\n{BitsSummary}\n\nВторой абзац.\n"),
        ("git/init-add-commit.md", $"{GitSummary}\n\nВторой абзац.\n"));

    private WebApplicationFactory<Program> WithLessons(TempLessons dir) =>
        _factory.WithWebHostBuilder(builder => builder.UseSetting("Content:LessonsPath", dir.Path));

    private static int Count(string html, string needle) => Regex.Matches(html, Regex.Escape(needle)).Count;

    private static string? Meta(string html, string attribute, string name)
    {
        var match = Regex.Match(html, $"<meta {attribute}=\"{Regex.Escape(name)}\" content=\"([^\"]*)\"");
        return match.Success ? match.Groups[1].Value : null;
    }

    private static string ItemFor(string html, string path)
    {
        var match = Regex.Match(html, $"<li [^>]*>(?:(?!</li>).)*href=\"{Regex.Escape(path)}\"(?:(?!</li>).)*</li>", RegexOptions.Singleline);
        Assert.True(match.Success, $"No li for {path}");
        return match.Value;
    }

    [Fact]
    public async Task GetHome_WithTwoLessons_ShowsExactlyTwoBadgesOnLessonedTopics()
    {
        // Arrange
        using var dir = CreateLessons();
        using var client = WithLessons(dir).CreateClient();

        // Act
        var html = await client.GetStringAsync("/");

        // Assert
        Assert.Equal(2, Count(html, "lesson-badge"));
        Assert.Contains("lesson-badge", ItemFor(html, "/fundamentals/binary-bits-bytes"));
        Assert.Contains("lesson-badge", ItemFor(html, "/git/init-add-commit"));
        Assert.DoesNotContain("lesson-badge", ItemFor(html, "/fundamentals/cpu-memory-disk"));
    }

    [Fact]
    public async Task GetSection_WithLesson_BadgeOnlyOnLessonedTopic()
    {
        // Arrange
        using var dir = CreateLessons();
        using var client = WithLessons(dir).CreateClient();

        // Act
        var html = await client.GetStringAsync("/fundamentals");

        // Assert
        Assert.Equal(1, Count(html, "lesson-badge"));
        Assert.Contains("lesson-badge", ItemFor(html, "/fundamentals/binary-bits-bytes"));
        Assert.DoesNotContain("lesson-badge", ItemFor(html, "/fundamentals/cpu-memory-disk"));
    }

    [Fact]
    public async Task GetHome_DefaultConfigWithoutLessons_HasNoBadges()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var html = await client.GetStringAsync("/");

        // Assert
        Assert.DoesNotContain("lesson-badge", html);
    }

    [Fact]
    public async Task GetTopic_WithLesson_DescriptionAndOgDescriptionEqualTopLevelSummary()
    {
        // Arrange
        using var dir = CreateLessons();
        using var client = WithLessons(dir).CreateClient();

        // Act
        var html = await client.GetStringAsync("/fundamentals/binary-bits-bytes");
        var description = Meta(html, "name", "description");
        var og = Meta(html, "property", "og:description");

        // Assert
        Assert.NotNull(description);
        Assert.DoesNotContain("&amp;amp;", description);
        Assert.Contains("&amp;", description);
        Assert.Equal(BitsSummary, WebUtility.HtmlDecode(description));
        Assert.Equal(description, og);
    }

    [Fact]
    public async Task GetTopic_WithoutLesson_KeepsCatalogDescription()
    {
        // Arrange
        using var dir = CreateLessons();
        using var client = WithLessons(dir).CreateClient();

        // Act
        var html = await client.GetStringAsync("/fundamentals/cpu-memory-disk");
        var description = WebUtility.HtmlDecode(Meta(html, "name", "description")!);

        // Assert
        Assert.Contains("Отмечайте изученное", description);
    }
}
