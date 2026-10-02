using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc.Testing;
using ProgChecklist.Core;

namespace ProgChecklist.Web.Tests;

public class SeoTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string BaseUrl = "https://example.test";
    private static readonly XNamespace SitemapNs = "http://www.sitemaps.org/schemas/sitemap/0.9";

    private readonly WebApplicationFactory<Program> _factory;
    private readonly WebApplicationFactory<Program> _configuredFactory;

    public SeoTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _configuredFactory = factory.WithWebHostBuilder(builder => builder.UseSetting("Site:BaseUrl", BaseUrl));
    }

    private static ITopicCatalog LoadCatalog() =>
        JsonTopicCatalog.LoadFromFile(Path.Combine(AppContext.BaseDirectory, "content", "topics.json"));

    private static string Decode(string html) => WebUtility.HtmlDecode(html);

    private static string? Match(string html, string pattern)
    {
        var match = Regex.Match(html, pattern, RegexOptions.Singleline);
        return match.Success ? Decode(match.Groups[1].Value) : null;
    }

    private static string? Canonical(string html) => Match(html, "<link rel=\"canonical\" href=\"([^\"]*)\"");

    private static string? Meta(string html, string attribute, string name) =>
        Match(html, $"<meta {attribute}=\"{Regex.Escape(name)}\" content=\"([^\"]*)\"");

    private static string? Title(string html) => Match(html, "<title>(.*?)</title>");

    private static int CountOccurrences(string html, string needle) =>
        Regex.Matches(html, Regex.Escape(needle)).Count;

    private static string FirstTopicPath()
    {
        var catalog = LoadCatalog();
        var section = catalog.GetSections()[0];
        return $"/{section.Slug}/{section.Groups[0].Topics[0].Slug}";
    }

    [Fact]
    public async Task GetSitemap_WithBaseUrl_ReturnsAll269UrlsInSitemapNamespace()
    {
        // Arrange
        using var client = _configuredFactory.CreateClient();

        // Act
        using var response = await client.GetAsync("/sitemap.xml");
        var xml = await response.Content.ReadAsStringAsync();
        var document = XDocument.Parse(xml);
        var locations = document.Root!.Elements(SitemapNs + "url").Select(url => url.Element(SitemapNs + "loc")!.Value).ToList();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/xml", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(SitemapNs + "urlset", document.Root.Name);
        Assert.Equal(1 + 14 + 254, locations.Count);
        Assert.Equal(locations.Count, locations.Distinct().Count());
        Assert.All(locations, location => Assert.StartsWith(BaseUrl + "/", location));
        Assert.Contains(BaseUrl + "/", locations);
        Assert.Contains(BaseUrl + FirstTopicPath(), locations);
    }

    [Fact]
    public async Task GetRobots_WithBaseUrl_ReturnsPlainTextWithSitemapLine()
    {
        // Arrange
        using var client = _configuredFactory.CreateClient();

        // Act
        using var response = await client.GetAsync("/robots.txt");
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/plain", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("User-agent: *", body);
        Assert.Contains("Sitemap: https://example.test/sitemap.xml", body);
    }

    [Fact]
    public async Task Get_HomeSectionAndTopic_CanonicalAndOgUrlMatchPath()
    {
        // Arrange
        using var client = _configuredFactory.CreateClient();
        var section = LoadCatalog().GetSections()[0];
        var paths = new[] { "/", $"/{section.Slug}", FirstTopicPath() };

        foreach (var path in paths)
        {
            // Act
            var html = await client.GetStringAsync(path);

            // Assert
            Assert.Equal(BaseUrl + path, Canonical(html));
            Assert.Equal(BaseUrl + path, Meta(html, "property", "og:url"));
        }
    }

    [Fact]
    public async Task Get_SectionWithTrailingSlash_CanonicalHasNoTrailingSlash()
    {
        // Arrange
        using var client = _configuredFactory.CreateClient();

        // Act
        var html = await client.GetStringAsync("/fundamentals/");

        // Assert
        Assert.Equal(BaseUrl + "/fundamentals", Canonical(html));
    }

    [Fact]
    public async Task Get_HomeWithoutBaseUrl_CanonicalUsesRequestHost()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var html = await client.GetStringAsync("/");

        // Assert
        Assert.StartsWith("http://localhost", Canonical(html));
    }

    [Fact]
    public async Task Get_AllPages_TitlesAndDescriptionsAreUnique()
    {
        await AssertTitlesAndDescriptionsAreUniqueAsync(_configuredFactory);
    }

    [Fact]
    public async Task Get_AllPagesWithLessons_TitlesAndDescriptionsAreUnique()
    {
        // Arrange
        using var dir = new TempLessons(
            ("fundamentals/binary-bits-bytes.md", "Первый абзац урока про биты.\n"),
            ("git/init-add-commit.md", "Первый абзац урока про коммиты.\n"));
        var factory = _configuredFactory.WithWebHostBuilder(builder => builder.UseSetting("Content:LessonsPath", dir.Path));

        // Act & Assert
        await AssertTitlesAndDescriptionsAreUniqueAsync(factory);
    }

    private static async Task AssertTitlesAndDescriptionsAreUniqueAsync(WebApplicationFactory<Program> factory)
    {
        // Arrange
        using var client = factory.CreateClient();
        var paths = SitemapPaths.All(LoadCatalog());
        var titles = new HashSet<string>();
        var descriptions = new HashSet<string>();

        foreach (var path in paths)
        {
            // Act
            var html = await client.GetStringAsync(path);

            // Assert
            Assert.True(titles.Add(Title(html)!), $"Duplicate title on {path}");
            Assert.True(descriptions.Add(Meta(html, "name", "description")!), $"Duplicate description on {path}");
        }

        Assert.Equal(paths.Count, titles.Count);
    }

    [Fact]
    public async Task Get_AllPages_HaveExactlyOneDescriptionUpTo160Chars()
    {
        // Arrange
        using var client = _configuredFactory.CreateClient();

        foreach (var path in SitemapPaths.All(LoadCatalog()))
        {
            // Act
            var html = await client.GetStringAsync(path);
            var description = Meta(html, "name", "description");

            // Assert
            Assert.Equal(1, CountOccurrences(html, "<meta name=\"description\""));
            Assert.False(string.IsNullOrWhiteSpace(description), $"Empty description on {path}");
            Assert.True(description.Length <= 160, $"Description too long on {path}");
        }
    }

    [Fact]
    public async Task Get_HomeAndTopic_OgTypeIsWebsiteAndArticle()
    {
        // Arrange
        using var client = _configuredFactory.CreateClient();

        // Act
        var home = await client.GetStringAsync("/");
        var topic = await client.GetStringAsync(FirstTopicPath());

        // Assert
        Assert.Equal("website", Meta(home, "property", "og:type"));
        Assert.Equal("article", Meta(topic, "property", "og:type"));
    }

    [Fact]
    public async Task Get_Topic_ContainsBreadcrumbListJsonLd()
    {
        // Arrange
        using var client = _configuredFactory.CreateClient();
        var catalog = LoadCatalog();
        var section = catalog.GetSections()[0];
        var topic = section.Groups[0].Topics[0];

        // Act
        var html = await client.GetStringAsync($"/{section.Slug}/{topic.Slug}");
        var json = Regex.Match(html, "<script type=\"application/ld\\+json\">(.*?)</script>", RegexOptions.Singleline).Groups[1].Value;
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var items = root.GetProperty("itemListElement").EnumerateArray().ToList();

        // Assert
        Assert.Equal("BreadcrumbList", root.GetProperty("@type").GetString());
        Assert.Equal(3, items.Count);
        Assert.Equal(new[] { 1, 2, 3 }, items.Select(item => item.GetProperty("position").GetInt32()));
        Assert.Equal(
            new[] { BaseUrl + "/", $"{BaseUrl}/{section.Slug}", $"{BaseUrl}/{section.Slug}/{topic.Slug}" },
            items.Select(item => item.GetProperty("item").GetString()));
        Assert.Equal(
            new[] { "Карта программиста", section.Title, topic.Title },
            items.Select(item => item.GetProperty("name").GetString()));
    }

    [Fact]
    public async Task Get_UnknownPage_HasNoIndexAndNoCanonical()
    {
        // Arrange
        using var client = _configuredFactory.CreateClient();

        // Act
        using var response = await client.GetAsync("/nope");
        var html = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("<meta name=\"robots\" content=\"noindex\">", html);
        Assert.DoesNotContain("rel=\"canonical\"", html);
        Assert.DoesNotContain("og:url", html);
    }

    [Theory]
    [InlineData("not a url")]
    [InlineData("https://x.test/path")]
    [InlineData("ftp://x.test")]
    [InlineData("https://x.test?q=1")]
    [InlineData("https://x.test/#frag")]
    [InlineData("https://user@x.test")]
    public void CreateClient_InvalidBaseUrl_ThrowsAtStartup(string baseUrl)
    {
        // Arrange
        var factory = _factory.WithWebHostBuilder(builder => builder.UseSetting("Site:BaseUrl", baseUrl));

        // Act & Assert
        Assert.ThrowsAny<Exception>(() => factory.CreateClient());
    }

    [Fact]
    public async Task Get_BaseUrlWithTrailingSlash_IsNormalized()
    {
        // Arrange
        var factory = _factory.WithWebHostBuilder(builder => builder.UseSetting("Site:BaseUrl", BaseUrl + "/"));
        using var client = factory.CreateClient();

        // Act
        var html = await client.GetStringAsync("/");

        // Assert
        Assert.Equal(BaseUrl + "/", Canonical(html));
    }
}
