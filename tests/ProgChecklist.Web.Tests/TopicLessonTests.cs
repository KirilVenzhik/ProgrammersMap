using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ProgChecklist.Web.Tests;

public class TopicLessonTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string LessonPath = "git/init-add-commit.md";
    private const string LessonUrl = "/git/init-add-commit";
    private const string OtherTopicUrl = "/git/branching-and-merging";
    private const string StubText = "Урок в разработке";

    private const string LessonMarkdown = """
        ## Введение

        Если a < b & c, то **жирный** текст.

        ```html
        <div>
        ```

        - первый
        - второй

        | a | b |
        |---|---|
        | 1 | 2 |

        <script>alert(1)</script>
        """;

    private readonly WebApplicationFactory<Program> _factory;

    public TopicLessonTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private static async Task<(HttpStatusCode Status, string Body)> GetAsync(WebApplicationFactory<Program> factory, string url)
    {
        using var client = factory.CreateClient();
        var response = await client.GetAsync(url);
        return (response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    private WebApplicationFactory<Program> WithLesson(TempLessons dir) =>
        _factory.WithWebHostBuilder(builder => builder.UseSetting("Content:LessonsPath", dir.Path));

    [Fact]
    public async Task Get_TopicWithLesson_RendersLessonInsteadOfStub()
    {
        // Arrange
        using var dir = new TempLessons(LessonPath, LessonMarkdown);
        var factory = WithLesson(dir);

        // Act
        var (status, body) = await GetAsync(factory, LessonUrl);

        // Assert
        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Contains("class=\"lesson lesson-body\"", body);
        Assert.Matches("<h2 id=\"[^\"]+\">Введение</h2>", body);
        Assert.Contains("<strong>жирный</strong>", body);
        Assert.Contains("<table>", body);
        Assert.DoesNotContain(StubText, body);
    }

    [Fact]
    public async Task Get_TopicWithoutLesson_ShowsStub()
    {
        // Arrange
        using var dir = new TempLessons(LessonPath, LessonMarkdown);
        var factory = WithLesson(dir);

        // Act
        var (status, body) = await GetAsync(factory, OtherTopicUrl);

        // Assert
        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Contains(StubText, body);
        Assert.DoesNotContain("lesson-body", body);
    }

    [Fact]
    public async Task Get_TopicWithLesson_DoesNotDoubleEscapeHtml()
    {
        // Arrange
        using var dir = new TempLessons(LessonPath, LessonMarkdown);
        var factory = WithLesson(dir);

        // Act
        var (_, body) = await GetAsync(factory, LessonUrl);

        // Assert
        Assert.Contains("a &lt; b &amp; c", body);
        Assert.Contains("&lt;div&gt;", body);
        Assert.DoesNotContain("&amp;lt;", body);
        Assert.DoesNotContain("&amp;amp;", body);
        Assert.DoesNotContain("&lt;strong&gt;", body);
    }

    [Fact]
    public async Task Get_LessonWithRawScriptTag_EscapesIt()
    {
        // Arrange
        using var dir = new TempLessons(LessonPath, LessonMarkdown);
        var factory = WithLesson(dir);

        // Act
        var (_, body) = await GetAsync(factory, LessonUrl);

        // Assert
        Assert.DoesNotContain("<script>alert", body);
        Assert.Contains("&lt;script&gt;alert(1)&lt;/script&gt;", body);
    }

    [Fact]
    public async Task Get_MissingLessonsFolder_ShowsStub()
    {
        // Arrange (missing lessons folder = no lessons, independent of the real content)
        var factory = _factory.WithoutLessons();

        // Act
        var (status, body) = await GetAsync(factory, LessonUrl);

        // Assert
        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Contains(StubText, body);
        Assert.DoesNotContain("lesson-body", body);
    }
}
