using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ProgChecklist.Web.Tests;

public class LayoutTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string FontsStylesheet = "https://fonts.googleapis.com/css2?family=Golos+Text";

    private readonly WebApplicationFactory<Program> _factory;

    public LayoutTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_SiteCss_ReturnsCss()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        using var response = await client.GetAsync("/css/site.css");
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/css", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("--hl:", body);
        Assert.Contains("prefers-color-scheme", body);
    }

    [Fact]
    public async Task Get_Root_UsesLayout()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var body = await client.GetStringAsync("/");

        // Assert
        Assert.Contains("lang=\"ru\"", body);
        Assert.Contains("/css/site.css?v=", body);
        Assert.Contains(FontsStylesheet, body);
        Assert.Contains("<footer", body);
    }

    [Fact]
    public async Task Get_Root_HasNoExternalResourcesExceptGoogleFonts()
    {
        // Arrange
        using var client = _factory.CreateClient();
        var allowed = new[] { "https://fonts.googleapis.com", "https://fonts.gstatic.com" };

        // Act
        var body = await client.GetStringAsync("/");
        var bodyWithoutCanonical = Regex.Replace(body, "<link rel=\"canonical\"[^>]*>", "");
        var urls = Regex.Matches(bodyWithoutCanonical, "(?:src|href|srcset|action|poster)\\s*=\\s*[\"']([^\"']*)[\"']", RegexOptions.IgnoreCase)
            .Select(m => m.Groups[1].Value)
            .Where(u => u.StartsWith("http://") || u.StartsWith("https://") || u.StartsWith("//"))
            .ToList();

        // Assert
        Assert.NotEmpty(urls);
        foreach (var url in urls)
        {
            Assert.True(allowed.Any(a => url.StartsWith(a, StringComparison.Ordinal)), $"External resource not allowed: {url}");
        }
        Assert.DoesNotMatch("<script[^>]*src=\"(https?:)?//", body);
    }
}
