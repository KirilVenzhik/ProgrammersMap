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
    public async Task Get_Root_EachRowHasOneCheckboxWithAriaLabel()
    {
        // Arrange
        var catalog = Catalog;

        // Act
        var body = WebUtility.HtmlDecode(await GetRootAsync());

        // Assert
        Assert.Equal(catalog.TopicCount, Count(body, "<li data-key="));
        Assert.Equal(catalog.TopicCount, Count(body, "type=\"checkbox\""));
        Assert.Equal(catalog.TopicCount, Count(body, "aria-label=\"Изучено: "));
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
    public async Task Get_Root_HasNoScriptTag()
    {
        // Arrange & Act
        var body = await GetRootAsync();

        // Assert
        Assert.DoesNotContain("<script", body, StringComparison.OrdinalIgnoreCase);
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
