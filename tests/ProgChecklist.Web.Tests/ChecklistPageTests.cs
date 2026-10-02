using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ProgChecklist.Core;

namespace ProgChecklist.Web.Tests;

public class ChecklistPageTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ChecklistPageTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private ITopicCatalog Catalog => _factory.Services.GetRequiredService<ITopicCatalog>();

    private async Task<string> GetRootAsync()
    {
        using var client = _factory.CreateClient();
        return await client.GetStringAsync("/");
    }

    private static int Count(string body, string needle)
    {
        var count = 0;
        var index = 0;
        while ((index = body.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }

        return count;
    }

    [Fact]
    public async Task Get_Root_RendersAllTopics()
    {
        // Arrange
        var catalog = Catalog;

        // Act
        var body = await GetRootAsync();
        var decoded = WebUtility.HtmlDecode(body);

        // Assert
        Assert.Equal(254, catalog.TopicCount);
        foreach (var section in catalog.GetSections())
        {
            foreach (var topic in section.Groups.SelectMany(g => g.Topics))
            {
                Assert.Contains($"href=\"/{section.Slug}/{topic.Slug}\"", body);
                Assert.Contains($"data-key=\"{section.Slug}/{topic.Slug}\"", body);
                Assert.Contains(topic.Title, decoded);
            }
        }

        Assert.Equal(catalog.TopicCount, Count(body, "<li data-key="));
        Assert.Equal(catalog.TopicCount, Count(body, "<span data-key="));
    }

    [Fact]
    public async Task Get_Root_EachRowHasCheckboxWithAssociatedLabel()
    {
        // Arrange
        var catalog = Catalog;

        // Act
        var body = WebUtility.HtmlDecode(await GetRootAsync());

        // Assert
        Assert.Equal(catalog.TopicCount, Count(body, "<li data-key="));
        Assert.Equal(catalog.TopicCount, Count(body, "<label class=\"chk\""));
        foreach (var section in catalog.GetSections())
        {
            foreach (var topic in section.Groups.SelectMany(g => g.Topics))
            {
                var id = $"cb-{section.Slug}-{topic.Slug}";
                Assert.Contains($"<label class=\"chk\" for=\"{id}\"><input type=\"checkbox\" id=\"{id}\"", body);
                Assert.Contains($"<span class=\"visually-hidden\">Изучено: {topic.Title}</span>", body);
            }
        }
    }

    [Fact]
    public async Task Get_Root_SidebarHasLinkPerSection()
    {
        // Arrange
        var sections = Catalog.GetSections();

        // Act
        var body = await GetRootAsync();

        // Assert
        Assert.Equal(14, sections.Count);
        foreach (var section in sections)
        {
            Assert.Contains($"<a href=\"#{section.Slug}\">", body);
        }
    }

    [Fact]
    public async Task Get_Root_ShowsZeroProgressStat()
    {
        // Arrange & Act
        var body = WebUtility.HtmlDecode(await GetRootAsync());

        // Assert
        Assert.Contains("0 из 254 тем", body);
    }

    [Fact]
    public async Task Get_Root_HasSingleModuleScript()
    {
        // Arrange & Act
        var body = await GetRootAsync();

        // Assert
        Assert.Equal(1, Count(body.ToLowerInvariant(), "<script"));
        Assert.Matches("<script type=\"module\" src=\"/js/progress\\.js\\?v=[^\"]+\"></script>", body);
    }

    [Fact]
    public async Task Get_ProgressScript_ReturnsJavaScript()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/js/progress.js");
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("javascript", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("prog-checklist-v2", body);
    }

    [Fact]
    public async Task Get_Root_RendersHiddenToolbar()
    {
        // Arrange & Act
        var body = await GetRootAsync();

        // Assert
        Assert.Contains("id=\"toolbar\" hidden", body);
        Assert.Contains("id=\"q\"", body);
        Assert.Equal(4, Count(body, "<button type=\"button\" data-level=\""));
        Assert.Contains("id=\"hide\"", body);
        Assert.Contains("id=\"reset\"", body);
        Assert.Contains("id=\"empty\" hidden", body);
    }

    [Fact]
    public async Task Get_Root_HasSkipLinkToMain()
    {
        // Arrange & Act
        var body = await GetRootAsync();

        // Assert
        Assert.Contains("href=\"#main\"", body);
        Assert.Contains("<main id=\"main\"", body);
    }
}
