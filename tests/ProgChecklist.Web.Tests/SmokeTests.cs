using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ProgChecklist.Core;

namespace ProgChecklist.Web.Tests;

public class SmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SmokeTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_Root_ReturnsOkWithFirstSectionTitle()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        using var response = await client.GetAsync("/");
        var body = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Основы программирования", body);
    }

    [Fact]
    public async Task Get_Root_RendersCyrillicWithoutEntities()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var rawBody = await client.GetStringAsync("/");

        // Assert
        Assert.Contains("Основы программирования", rawBody);
    }

    [Fact]
    public async Task Get_Root_ListsAllSectionsWithCounts()
    {
        // Arrange
        var sections = _factory.Services.GetRequiredService<ITopicCatalog>().GetSections();
        using var client = _factory.CreateClient();

        // Act
        var body = await client.GetStringAsync("/");

        // Assert
        Assert.Equal(14, sections.Count);
        foreach (var section in sections)
        {
            Assert.Contains($"id=\"{section.Slug}\"", body);
            Assert.Contains($"data-sc=\"{section.Slug}\">0 / {section.TopicCount}<", body);
        }
    }

    [Fact]
    public void TopicCatalog_ResolvedTwice_ReturnsSameInstance()
    {
        // Arrange
        var services = _factory.Services;

        // Act
        var first = services.GetRequiredService<ITopicCatalog>();
        var second = services.GetRequiredService<ITopicCatalog>();

        // Assert
        Assert.Same(first, second);
    }
}
