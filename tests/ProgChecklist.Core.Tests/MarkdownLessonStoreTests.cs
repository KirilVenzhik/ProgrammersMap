namespace ProgChecklist.Core.Tests;

public class MarkdownLessonStoreTests
{
    private const string TopicsJson = """
        {
          "sections": [
            {
              "slug": "tools", "title": "Tools", "order": 1,
              "groups": [
                { "slug": "vcs", "title": "VCS", "topics": [
                  { "slug": "git", "title": "Git", "level": "junior" },
                  { "slug": "branches", "title": "Branches", "level": "junior" }
                ] }
              ]
            }
          ]
        }
        """;

    private static readonly ITopicCatalog Topics = JsonTopicCatalog.Parse(TopicsJson);

    private static MarkdownLessonStore Load(string markdown, string section = "tools", string topic = "git") =>
        MarkdownLessonStore.FromSources([(section, topic, markdown)], Topics);

    private static string Html(string markdown) => Load(markdown).FindLesson("tools", "git")!.Html;

    private static LessonStoreException Fail(string markdown, string section = "tools", string topic = "git") =>
        Assert.Throws<LessonStoreException>(() => Load(markdown, section, topic));

    [Fact]
    public void FromSources_Markdown_RendersHeadingWithId()
    {
        var html = Html("## Hello World\n\ntext");

        Assert.Contains("<h2 id=\"hello-world\">Hello World</h2>", html);
    }

    [Fact]
    public void FromSources_Markdown_RendersListCodeTableEmphasis()
    {
        var markdown = "## T\n\n- one\n- two\n\n```csharp\nvar x = 1;\n```\n\n| a | b |\n|---|---|\n| 1 | 2 |\n\n**bold** and ~~gone~~\n";

        var html = Html(markdown);

        Assert.Contains("<ul>", html);
        Assert.Contains("<li>one</li>", html);
        Assert.Contains("<pre><code class=\"language-csharp\">", html);
        Assert.Contains("<table>", html);
        Assert.Contains("<strong>bold</strong>", html);
        Assert.Contains("<del>gone</del>", html);
    }

    [Fact]
    public void FromSources_RawHtmlBlock_IsEscaped()
    {
        var html = Html("## T\n\n<script>alert(1)</script>\n");

        Assert.DoesNotContain("<script>", html);
        Assert.Contains("&lt;script&gt;", html);
    }

    [Fact]
    public void FromSources_InlineRawHtml_IsEscaped()
    {
        var html = Html("## T\n\ntext <img src=x onerror=alert(1)> more\n");

        Assert.DoesNotContain("<img", html);
        Assert.Contains("&lt;img", html);
    }

    [Fact]
    public void FromSources_UnknownTopic_Throws()
    {
        var ex = Fail("## T\n\ntext", topic: "nope");

        Assert.Contains("tools/nope", ex.Message);
    }

    [Fact]
    public void FromSources_NonCanonicalCase_Throws()
    {
        var ex = Fail("## T\n\ntext", section: "Tools");

        Assert.Contains("Tools/git", ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   \n\n  ")]
    public void FromSources_EmptyFile_Throws(string markdown)
    {
        var ex = Fail(markdown);

        Assert.Contains("tools/git", ex.Message);
    }

    [Theory]
    [InlineData("# Title\n\ntext")]
    [InlineData("Title\n===\n\ntext")]
    public void FromSources_LevelOneHeading_Throws(string markdown)
    {
        var ex = Fail(markdown);

        Assert.Contains("tools/git", ex.Message);
        Assert.Contains("level-1", ex.Message);
    }

    [Theory]
    [InlineData("## T\n\n[x](javascript:alert(1))")]
    [InlineData("## T\n\n![x](data:image/png;base64,AAA)")]
    [InlineData("## T\n\n[x](vbscript:msgbox)")]
    [InlineData("## T\n\n[x](JaVaScRiPt:alert(1))")]
    [InlineData("## T\n\n<javascript:alert(1)>")]
    public void FromSources_DisallowedScheme_Throws(string markdown)
    {
        var ex = Fail(markdown);

        Assert.Contains("tools/git", ex.Message);
        Assert.Contains("scheme", ex.Message);
    }

    [Theory]
    [InlineData("[x](other.md)")]
    [InlineData("[x](../branches)")]
    [InlineData("[x](#anchor)")]
    [InlineData("[x](https://example.test/a?b=c:d)")]
    [InlineData("[x](http://example.test)")]
    [InlineData("[x](mailto:a@example.test)")]
    [InlineData("![x](/img/a.png)")]
    public void FromSources_AllowedLinks_Load(string link)
    {
        var store = Load("## T\n\n" + link);

        Assert.Equal(1, store.Count);
    }

    [Fact]
    public void Summary_StripsMarkdownSyntax()
    {
        var store = Load("**Bold** and [link](https://x.test)");

        Assert.Equal("Bold and link", store.FindLesson("tools", "git")!.Summary);
    }

    [Fact]
    public void Summary_HeadingThenParagraph_UsesParagraphWithCollapsedWhitespace()
    {
        var store = Load("## Heading\n\nFirst   line\nsecond `code` line.\n\nOther paragraph.");

        Assert.Equal("First line second code line.", store.FindLesson("tools", "git")!.Summary);
    }

    [Fact]
    public void Summary_NoParagraph_IsEmpty()
    {
        var store = Load("## Only heading");

        Assert.Equal(string.Empty, store.FindLesson("tools", "git")!.Summary);
    }

    [Fact]
    public void Lookup_IsCaseInsensitive_AndHasLessonWorks()
    {
        var store = Load("## T\n\ntext");

        Assert.NotNull(store.FindLesson("TOOLS", "Git"));
        Assert.True(store.HasLesson("tools", "git"));
        Assert.False(store.HasLesson("tools", "branches"));
        Assert.Null(store.FindLesson(" ", "git"));
        Assert.Null(store.FindLesson("tools", null!));
    }

    [Fact]
    public void FromSources_DuplicateKey_Throws()
    {
        Assert.Throws<LessonStoreException>(() => MarkdownLessonStore.FromSources(
            [("tools", "git", "## A\n\nx"), ("tools", "git", "## B\n\ny")], Topics));
    }

    [Fact]
    public void LoadFromDirectory_MissingRoot_IsEmpty()
    {
        var root = Path.Combine(Path.GetTempPath(), $"lessons-missing-{Guid.NewGuid():N}");

        var store = MarkdownLessonStore.LoadFromDirectory(root, Topics);

        Assert.Equal(0, store.Count);
    }

    [Fact]
    public void LoadFromDirectory_ValidTree_LoadsLessonsAndIgnoresOtherFiles()
    {
        using var dir = new TempDirectory();
        dir.Write(Path.Combine("tools", "git.md"), "## T\n\nHello.");
        dir.Write(Path.Combine("tools", "notes.txt"), "ignored");
        dir.Write(".gitkeep", "");

        var store = MarkdownLessonStore.LoadFromDirectory(dir.Path, Topics);

        Assert.Equal(1, store.Count);
        Assert.Equal("Hello.", store.FindLesson("tools", "git")!.Summary);
    }

    [Fact]
    public void LoadFromDirectory_NonCanonicalFileName_Throws()
    {
        using var dir = new TempDirectory();
        dir.Write(Path.Combine("tools", "Git.md"), "## T\n\ntext");

        var ex = Assert.Throws<LessonStoreException>(() => MarkdownLessonStore.LoadFromDirectory(dir.Path, Topics));

        Assert.Contains("tools/Git", ex.Message);
    }

    [Fact]
    public void LoadFromDirectory_NonCanonicalDirectoryName_Throws()
    {
        using var dir = new TempDirectory();
        dir.Write(Path.Combine("Tools", "git.md"), "## T\n\ntext");

        Assert.Throws<LessonStoreException>(() => MarkdownLessonStore.LoadFromDirectory(dir.Path, Topics));
    }

    [Theory]
    [InlineData("git.md")]
    [InlineData("tools/vcs/git.md")]
    public void LoadFromDirectory_WrongDepth_Throws(string relativePath)
    {
        using var dir = new TempDirectory();
        dir.Write(relativePath.Replace('/', Path.DirectorySeparatorChar), "## T\n\ntext");

        var ex = Assert.Throws<LessonStoreException>(() => MarkdownLessonStore.LoadFromDirectory(dir.Path, Topics));

        Assert.Contains("git.md", ex.Message);
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"lessons-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Write(string relativePath, string content)
        {
            var full = System.IO.Path.Combine(Path, relativePath);
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(full)!);
            File.WriteAllText(full, content);
        }

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
