using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using ProgChecklist.Core;

namespace ProgChecklist.Web.Tests;

public class SectionPageTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SectionPageTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private static ITopicCatalog LoadCatalog() =>
        JsonTopicCatalog.LoadFromFile(Path.Combine(AppContext.BaseDirectory, "content", "topics.json"));

    public static IEnumerable<object[]> SectionSlugs() =>
        LoadCatalog().GetSections().Select(section => new object[] { section.Slug });

    [Theory]
    [MemberData(nameof(SectionSlugs))]
    public async Task Get_SectionSlug_ReturnsOnlyThatSectionTopics(string slug)
    {
        var catalog = LoadCatalog();
        var section = catalog.FindSection(slug)!;
        var other = catalog.GetSections().First(s => s.Slug != slug);
        var otherTopic = other.Groups[0].Topics[0];
        using var client = _factory.CreateClient();

        var response = await client.GetAsync($"/{slug}");
        var body = System.Net.WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(section.Title, body);
        foreach (var topic in section.Groups.SelectMany(group => group.Topics))
        {
            Assert.Contains($"href=\"/{slug}/{topic.Slug}\"", body);
        }

        Assert.DoesNotContain($"href=\"/{other.Slug}/{otherTopic.Slug}\"", body);
    }

    [Fact]
    public async Task Get_UnknownSection_Returns404WithFriendlyPage()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/does-not-exist");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("Страница не найдена", body);
    }

    [Fact]
    public async Task Get_ErrorRoute_ReturnsStatusFromRoute()
    {
        using var client = _factory.CreateClient();

        // 500 can only come from the Error page itself, so this proves the literal route wins.
        var response = await client.GetAsync("/Error/500");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task Get_UppercaseSlug_RedirectsPermanentlyToCanonical()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/Fundamentals");

        Assert.Equal(HttpStatusCode.MovedPermanently, response.StatusCode);
        Assert.Equal("/fundamentals", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Get_SectionPage_HasToolbarStatsAndGroupHeadingsAsH2()
    {
        var section = LoadCatalog().FindSection("fundamentals")!;
        using var client = _factory.CreateClient();

        var body = await client.GetStringAsync("/fundamentals");

        Assert.Contains("id=\"toolbar\"", body);
        Assert.Contains("id=\"pct\"", body);
        Assert.Contains($"<span id=\"count\">0 из {section.TopicCount} тем</span>", body);
        Assert.Contains("<nav class=\"crumbs\"", body);
        Assert.Contains("<a href=\"/\">", body);
        Assert.DoesNotContain("<h3>", body);
        Assert.Contains($"<h2>{section.Groups[0].Title}</h2>", System.Net.WebUtility.HtmlDecode(body));
    }

    [Fact]
    public async Task Get_Root_SectionHeadingsLinkToSectionPages()
    {
        var sections = LoadCatalog().GetSections();
        using var client = _factory.CreateClient();

        var body = await client.GetStringAsync("/");

        foreach (var section in sections)
        {
            Assert.Matches(new Regex($"<h2><a href=\"/{Regex.Escape(section.Slug)}\">"), body);
        }
    }

    [Theory]
    [InlineData("/css/site.css")]
    [InlineData("/js/progress.js")]
    [InlineData("/")]
    public async Task Get_StaticAssetsAndHome_StillReturn200(string path)
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
