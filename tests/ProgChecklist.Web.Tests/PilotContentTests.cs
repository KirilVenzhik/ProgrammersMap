using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ProgChecklist.Web.Tests;

/// <summary>Checks the real pilot content shipped in <c>content/</c> (P2-07), not fixtures.</summary>
public class PilotContentTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public PilotContentTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    public static IEnumerable<object[]> LessonTopics() =>
    [
        ["git", "init-add-commit"],
        ["databases", "join"],
        ["fundamentals", "binary-bits-bytes"],
    ];

    public static IEnumerable<object[]> ResourceTopics() =>
    [
        ["git", "init-add-commit"],
        ["git", "branching-and-merging"],
        ["databases", "join"],
        ["databases", "indexes"],
        ["fundamentals", "binary-bits-bytes"],
        ["fundamentals", "encodings"],
        ["networking", "http-methods"],
        ["networking", "status-codes"],
        ["frontend", "flexbox"],
        ["security", "sql-injection"],
    ];

    private static string OpeningParagraph(string section, string topic)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "content", "lessons", section, topic + ".md");
        var text = File.ReadAllText(path).Replace("\r\n", "\n");
        return text.Split("\n\n")[0].Trim();
    }

    [Theory]
    [MemberData(nameof(LessonTopics))]
    public async Task Get_PilotTopic_RendersLessonNotStub(string section, string topic)
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var body = await client.GetStringAsync($"/{section}/{topic}");

        // Assert
        Assert.Contains("lesson-body", body);
        Assert.DoesNotContain("Урок в разработке", body);
    }

    [Theory]
    [MemberData(nameof(LessonTopics))]
    public async Task Get_PilotTopic_MetaDescriptionIsOpeningParagraph(string section, string topic)
    {
        // Arrange
        using var client = _factory.CreateClient();
        var expected = OpeningParagraph(section, topic);

        // Act
        var body = await client.GetStringAsync($"/{section}/{topic}");
        var match = Regex.Match(body, "<meta name=\"description\" content=\"([^\"]*)\"");

        // Assert
        Assert.True(match.Success);
        Assert.InRange(expected.Length, 80, 160);
        // The paragraphs contain no & < > or quotes and the site allows all Unicode ranges, so the raw value equals the text.
        Assert.DoesNotContain("&#", match.Groups[1].Value);
        Assert.Equal(expected, match.Groups[1].Value);
    }

    [Fact]
    public async Task GetHome_RealContent_ShowsExactlyThreeLessonBadges()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var html = await client.GetStringAsync("/");

        // Assert
        Assert.Equal(3, Regex.Matches(html, "lesson-badge").Count);
    }

    [Theory]
    [MemberData(nameof(ResourceTopics))]
    public async Task Get_PilotTopic_RendersResourcesSection(string section, string topic)
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync($"/{section}/{topic}");
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<section class=\"resources\"", body);
        Assert.Contains("Где изучить", body);
        Assert.True(Regex.Matches(body, @"<li>\s*<a href=").Count >= 2);
    }
}
