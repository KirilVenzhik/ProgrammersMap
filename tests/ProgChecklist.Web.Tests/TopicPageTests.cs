using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using ProgChecklist.Core;

namespace ProgChecklist.Web.Tests;

public class TopicPageTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public TopicPageTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private static ITopicCatalog LoadCatalog() =>
        JsonTopicCatalog.LoadFromFile(Path.Combine(AppContext.BaseDirectory, "content", "topics.json"));

    public static IEnumerable<object[]> AllTopics() =>
        LoadCatalog().GetSections().SelectMany(
            section => section.Groups.SelectMany(group => group.Topics),
            (section, topic) => new object[] { section.Slug, topic.Slug });

    private static string Decode(string html) => WebUtility.HtmlDecode(html);

    [Theory]
    [MemberData(nameof(AllTopics))]
    public async Task Get_EveryTopic_Returns200WithTitleCheckboxAndCrumb(string sectionSlug, string topicSlug)
    {
        var topic = LoadCatalog().FindTopic(sectionSlug, topicSlug)!;
        using var client = _factory.CreateClient();

        var response = await client.GetAsync($"/{sectionSlug}/{topicSlug}");
        var body = Decode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(topic.Title, body);
        Assert.Contains($"data-key=\"{sectionSlug}/{topicSlug}\"", body);
        Assert.Contains($"href=\"/{sectionSlug}\"", body);
    }

    [Theory]
    [InlineData("/fundamentals/no-such-topic")]
    [InlineData("/no-such-section/binary-bits-bytes")]
    [InlineData("/databases/binary-bits-bytes")]
    public async Task Get_UnknownOrMismatchedTopic_Returns404WithFriendlyPage(string path)
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(path);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("Страница не найдена", body);
    }

    [Theory]
    [InlineData("/Fundamentals/binary-bits-bytes")]
    [InlineData("/fundamentals/Binary-Bits-Bytes")]
    public async Task Get_NonCanonicalCase_RedirectsPermanentlyToCanonical(string path)
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.MovedPermanently, response.StatusCode);
        Assert.Equal("/fundamentals/binary-bits-bytes", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Get_FirstTopicOverall_HasNextButNoPrevious()
    {
        var first = LoadCatalog().GetSections()[0];
        var topic = first.Groups[0].Topics[0];
        using var client = _factory.CreateClient();

        var body = await client.GetStringAsync($"/{first.Slug}/{topic.Slug}");

        Assert.DoesNotContain("rel=\"prev\"", body);
        Assert.Contains("rel=\"next\"", body);
    }

    [Fact]
    public async Task Get_LastTopicOverall_HasPreviousButNoNext()
    {
        var last = LoadCatalog().GetSections()[^1];
        var topic = last.Groups[^1].Topics[^1];
        using var client = _factory.CreateClient();

        var body = await client.GetStringAsync($"/{last.Slug}/{topic.Slug}");

        Assert.Contains("rel=\"prev\"", body);
        Assert.DoesNotContain("rel=\"next\"", body);
    }

    [Fact]
    public async Task Get_LastTopicOfFirstSection_NextLinksToFirstTopicOfSecondSection()
    {
        var sections = LoadCatalog().GetSections();
        var lastOfFirst = sections[0].Groups[^1].Topics[^1];
        var firstOfSecond = sections[1].Groups[0].Topics[0];
        using var client = _factory.CreateClient();

        var body = await client.GetStringAsync($"/{sections[0].Slug}/{lastOfFirst.Slug}");

        Assert.Contains($"rel=\"next\" href=\"/{sections[1].Slug}/{firstOfSecond.Slug}\"", body);
    }

    [Fact]
    public async Task Get_TopicPage_ShowsPlaceholderTitleAndLevel()
    {
        var catalog = LoadCatalog();
        var section = catalog.FindSection("fundamentals")!;
        var topic = catalog.FindTopic("fundamentals", "cpu-memory-disk")!;
        using var client = _factory.WithoutLessons().CreateClient();

        var body = Decode(await client.GetStringAsync("/fundamentals/cpu-memory-disk"));

        Assert.Contains("Урок в разработке", body);
        Assert.Contains($"<title>{topic.Title} — {section.Title} — Карта программиста</title>", body);
        Assert.Contains($"<span class=\"lvl\">{topic.Level.ToDisplayName()}</span>", body);
    }

    [Fact]
    public async Task Get_Root_TopicLinksResolveTo200()
    {
        using var client = _factory.CreateClient();
        var home = await client.GetStringAsync("/");
        var links = Regex
            .Matches(home, "href=\"(/[a-z0-9-]+/[a-z0-9-]+)\"")
            .Select(match => match.Groups[1].Value)
            .Distinct()
            .Take(3)
            .ToList();

        Assert.Equal(3, links.Count);
        foreach (var link in links)
        {
            var response = await client.GetAsync(link);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
