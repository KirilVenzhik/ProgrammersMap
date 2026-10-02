using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ProgChecklist.Web.Tests;

public class TopicResourcesTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string TopicUrl = "/git/init-add-commit";
    private const string OtherTopicUrl = "/git/branching-and-merging";
    private const string SectionTitle = "Где изучить";

    private const string FullJson = """
        {
          "version": 1,
          "resources": {
            "git/init-add-commit": [
              { "title": "Git Docs", "url": "https://git-scm.com/doc", "kind": "docs", "lang": "en", "free": true, "affiliate": false },
              { "title": "Курс <Git> & Co", "url": "https://courses.example/git?a=1&b=2", "kind": "course", "lang": "ru", "free": false, "affiliate": true },
              { "title": "Видео про Git", "url": "https://video.example/git", "kind": "video", "lang": "ru", "free": true, "affiliate": false }
            ]
          }
        }
        """;

    private const string FreeOnlyJson = """
        {
          "version": 1,
          "resources": {
            "git/init-add-commit": [
              { "title": "Git Docs", "url": "https://git-scm.com/doc", "kind": "docs", "lang": "en", "free": true, "affiliate": false }
            ]
          }
        }
        """;

    private readonly WebApplicationFactory<Program> _factory;

    public TopicResourcesTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private async Task<(HttpStatusCode Status, string Body)> GetAsync(string json, string url)
    {
        using var resources = new TempResources(json);
        // A missing lessons folder means no lessons, so real content added later cannot affect these tests.
        var noLessons = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"no-lessons-{Guid.NewGuid():N}");
        using var factory = _factory.WithWebHostBuilder(b => b
            .UseSetting("Content:ResourcesPath", resources.Path)
            .UseSetting("Content:LessonsPath", noLessons));
        using var client = factory.CreateClient();
        var response = await client.GetAsync(url);
        return (response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Get_TopicWithResources_RendersSectionWithThreeItems()
    {
        // Arrange & Act
        var (status, body) = await GetAsync(FullJson, TopicUrl);

        // Assert
        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Contains("<section class=\"resources\" aria-labelledby=\"resources-title\">", body);
        Assert.Contains($"<h2 id=\"resources-title\">{SectionTitle}</h2>", body);
        Assert.Contains("<ul class=\"resource-list\">", body);
        Assert.Equal(3, Regex.Matches(body, "<li>\\s*<a href=").Count);
    }

    [Fact]
    public async Task Get_TopicWithResources_KeepsFileOrder()
    {
        // Arrange & Act
        var (_, body) = await GetAsync(FullJson, TopicUrl);

        // Assert
        var first = body.IndexOf("Git Docs", StringComparison.Ordinal);
        var second = body.IndexOf("Курс &lt;Git&gt;", StringComparison.Ordinal);
        var third = body.IndexOf("Видео про Git", StringComparison.Ordinal);
        Assert.True(first >= 0 && first < second && second < third);
    }

    [Fact]
    public async Task Get_TopicWithAffiliateResource_UsesSponsoredRelOnlyForIt()
    {
        // Arrange & Act
        var (_, body) = await GetAsync(FullJson, TopicUrl);

        // Assert
        Assert.Contains("<a href=\"https://git-scm.com/doc\" rel=\"noopener\"", body);
        Assert.Contains("<a href=\"https://video.example/git\" rel=\"noopener\"", body);
        Assert.Equal(1, CountOf(body, "rel=\"sponsored noopener\""));
        Assert.Equal(2, CountOf(body, "rel=\"noopener\""));
        Assert.DoesNotContain("target=\"_blank\"", body);
    }

    [Fact]
    public async Task Get_TopicWithAffiliateResource_ShowsLabelOnceAndNote()
    {
        // Arrange & Act
        var (_, body) = await GetAsync(FullJson, TopicUrl);

        // Assert
        Assert.Equal(1, CountOf(body, "<span class=\"resource-ad\">партнёрская ссылка</span>"));
        Assert.Contains("class=\"resources-note\"", body);
    }

    [Fact]
    public async Task Get_TopicWithOnlyNonAffiliateResources_HasNoNoteAndNoLabel()
    {
        // Arrange & Act
        var (_, body) = await GetAsync(FreeOnlyJson, TopicUrl);

        // Assert
        Assert.Contains("class=\"resources\"", body);
        Assert.DoesNotContain("resources-note", body);
        Assert.DoesNotContain("resource-ad", body);
    }

    [Fact]
    public async Task Get_TopicWithoutResources_RendersNoSection()
    {
        // Arrange & Act
        var (status, body) = await GetAsync(FullJson, OtherTopicUrl);

        // Assert
        Assert.Equal(HttpStatusCode.OK, status);
        Assert.DoesNotContain("class=\"resources\"", body);
        Assert.DoesNotContain(SectionTitle, body);
    }

    [Fact]
    public async Task Get_EmptyResourceFile_RendersNoSection()
    {
        // Arrange & Act
        var (_, body) = await GetAsync("{ \"version\": 1, \"resources\": {} }", TopicUrl);

        // Assert
        Assert.DoesNotContain("class=\"resources\"", body);
        Assert.DoesNotContain(SectionTitle, body);
    }

    [Fact]
    public async Task Get_ResourceTitleWithSpecialChars_IsEncodedOnce()
    {
        // Arrange & Act
        var (_, body) = await GetAsync(FullJson, TopicUrl);

        // Assert
        Assert.Contains("Курс &lt;Git&gt; &amp; Co", body);
        Assert.DoesNotContain("&amp;amp;", body);
        Assert.DoesNotContain("&amp;lt;", body);
        Assert.DoesNotContain("Курс <Git>", body);
    }

    [Fact]
    public async Task Get_TopicWithResources_ShowsKindLanguageAndPrice()
    {
        // Arrange & Act
        var (_, body) = await GetAsync(FullJson, TopicUrl);

        // Assert
        Assert.Contains("Документация · на английском · бесплатно", body);
        Assert.Contains("Курс · на русском · платно", body);
        Assert.Contains("Видео · на русском · бесплатно", body);
        Assert.Contains("hreflang=\"en\"", body);
        Assert.Contains("hreflang=\"ru\"", body);
    }

    [Fact]
    public async Task Get_TopicWithResourcesAndNoLesson_StillShowsLessonStubBeforeResources()
    {
        // Arrange & Act
        var (_, body) = await GetAsync(FullJson, TopicUrl);

        // Assert
        var stub = body.IndexOf("Урок в разработке", StringComparison.Ordinal);
        Assert.True(stub >= 0);
        Assert.True(stub < body.IndexOf(SectionTitle, StringComparison.Ordinal));
    }

    private static int CountOf(string text, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }
}
