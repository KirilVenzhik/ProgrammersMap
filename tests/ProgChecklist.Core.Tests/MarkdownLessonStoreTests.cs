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
    public void FromSources_HeadingMarkdown_RendersHeadingWithId()
    {
        // Arrange
        var markdown = "## Hello World\n\ntext";

        // Act
        var html = Html(markdown);

        // Assert
        Assert.Contains("<h2 id=\"hello-world\">Hello World</h2>", html);
    }

    [Fact]
    public void FromSources_RichMarkdown_RendersListCodeTableEmphasis()
    {
        // Arrange
        var markdown = "## T\n\n- one\n- two\n\n```csharp\nvar x = 1;\n```\n\n| a | b |\n|---|---|\n| 1 | 2 |\n\n**bold** and ~~gone~~\n";

        // Act
        var html = Html(markdown);

        // Assert
        Assert.Contains("<ul>", html);
        Assert.Contains("<li>one</li>", html);
        Assert.Contains("<pre><code class=\"language-csharp\">", html);
        Assert.Contains("<table>", html);
        Assert.Contains("<strong>bold</strong>", html);
        Assert.Contains("<del>gone</del>", html);
    }

    [Fact]
    public void FromSources_RawHtmlBlock_EscapesHtml()
    {
        // Arrange
        var markdown = "## T\n\n<script>alert(1)</script>\n";

        // Act
        var html = Html(markdown);

        // Assert
        Assert.DoesNotContain("<script>", html);
        Assert.Contains("&lt;script&gt;", html);
    }

    [Fact]
    public void FromSources_InlineRawHtml_EscapesHtml()
    {
        // Arrange
        var markdown = "## T\n\ntext <img src=x onerror=alert(1)> more\n";

        // Act
        var html = Html(markdown);

        // Assert
        Assert.DoesNotContain("<img", html);
        Assert.Contains("&lt;img", html);
    }

    [Fact]
    public void FromSources_UnknownTopic_ThrowsLessonStoreException()
    {
        // Arrange
        var markdown = "## T\n\ntext";

        // Act
        var ex = Fail(markdown, topic: "nope");

        // Assert
        Assert.Contains("tools/nope", ex.Message);
    }

    [Fact]
    public void FromSources_NonCanonicalCase_ThrowsLessonStoreException()
    {
        // Arrange
        var markdown = "## T\n\ntext";

        // Act
        var ex = Fail(markdown, section: "Tools");

        // Assert
        Assert.Contains("Tools/git", ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   \n\n  ")]
    public void FromSources_EmptyFile_ThrowsLessonStoreException(string markdown)
    {
        // Arrange (markdown comes from InlineData)

        // Act
        var ex = Fail(markdown);

        // Assert
        Assert.Contains("tools/git", ex.Message);
    }

    [Theory]
    [InlineData("# Title\n\ntext")]
    [InlineData("Title\n===\n\ntext")]
    public void FromSources_LevelOneHeading_ThrowsLessonStoreException(string markdown)
    {
        // Arrange (markdown comes from InlineData)

        // Act
        var ex = Fail(markdown);

        // Assert
        Assert.Contains("tools/git", ex.Message);
        Assert.Contains("level-1", ex.Message);
    }

    [Theory]
    [InlineData("## T\n\n[x](javascript:alert(1))")]
    [InlineData("## T\n\n![x](data:image/png;base64,AAA)")]
    [InlineData("## T\n\n[x](vbscript:msgbox)")]
    [InlineData("## T\n\n[x](JaVaScRiPt:alert(1))")]
    [InlineData("## T\n\n<javascript:alert(1)>")]
    [InlineData("## T\n\n[x][r]\n\n[r]: javascript:alert(1)")]
    [InlineData("## T\n\n[x](java&#115;cript:alert(1))")]
    public void FromSources_DisallowedScheme_ThrowsLessonStoreException(string markdown)
    {
        // Arrange (markdown comes from InlineData)

        // Act
        var ex = Fail(markdown);

        // Assert
        Assert.Contains("tools/git", ex.Message);
        Assert.Contains("scheme", ex.Message);
    }

    [Fact]
    public void FromSources_GenericAttributeSyntax_StaysText()
    {
        // Arrange
        var markdown = "## Heading {onclick=alert(1)}\n\nParagraph {onclick=alert(1)}\n";

        // Act
        var html = Html(markdown);

        // Assert
        Assert.DoesNotMatch("<[^>]*onclick=", html);
        Assert.Contains("{onclick=alert(1)}", html);
    }

    [Theory]
    [InlineData("[x](other.md)")]
    [InlineData("[x](../branches)")]
    [InlineData("[x](#anchor)")]
    [InlineData("[x](https://example.test/a?b=c:d)")]
    [InlineData("[x](http://example.test)")]
    [InlineData("[x](mailto:a@example.test)")]
    [InlineData("![x](/img/a.png)")]
    public void FromSources_AllowedLink_LoadsLesson(string link)
    {
        // Arrange
        var markdown = "## T\n\n" + link;

        // Act
        var store = Load(markdown);

        // Assert
        Assert.Equal(1, store.Count);
    }

    [Fact]
    public void Summary_InlineMarkdown_StripsSyntax()
    {
        // Arrange
        var markdown = "**Bold** and [link](https://x.test)";

        // Act
        var store = Load(markdown);

        // Assert
        Assert.Equal("Bold and link", store.FindLesson("tools", "git")!.Summary);
    }

    [Fact]
    public void Summary_HeadingThenParagraph_UsesParagraphWithCollapsedWhitespace()
    {
        // Arrange
        var markdown = "## Heading\n\nFirst   line\nsecond `code` line.\n\nOther paragraph.";

        // Act
        var store = Load(markdown);

        // Assert
        Assert.Equal("First line second code line.", store.FindLesson("tools", "git")!.Summary);
    }

    [Fact]
    public void Summary_NoParagraph_ReturnsEmpty()
    {
        // Arrange
        var markdown = "## Only heading";

        // Act
        var store = Load(markdown);

        // Assert
        Assert.Equal(string.Empty, store.FindLesson("tools", "git")!.Summary);
    }

    [Theory]
    [InlineData("> Quoted paragraph.\n\nTop level.")]
    [InlineData("- List item.\n\nTop level.")]
    public void Summary_NestedParagraphBeforeTopLevel_UsesTopLevelParagraph(string body)
    {
        // Arrange
        var markdown = "## T\n\n" + body;

        // Act
        var store = Load(markdown);

        // Assert
        Assert.Equal("Top level.", store.FindLesson("tools", "git")!.Summary);
    }

    [Fact]
    public void Summary_OnlyNestedParagraph_FallsBackToNestedParagraph()
    {
        // Arrange
        var markdown = "## T\n\n> Only quoted.";

        // Act
        var store = Load(markdown);

        // Assert
        Assert.Equal("Only quoted.", store.FindLesson("tools", "git")!.Summary);
    }

    [Fact]
    public void Summary_HtmlEntities_DecodesEntities()
    {
        // Arrange
        var markdown = "## T\n\nTom &amp; Jerry &copy; 2026";

        // Act
        var store = Load(markdown);

        // Assert
        Assert.Equal("Tom & Jerry © 2026", store.FindLesson("tools", "git")!.Summary);
    }

    [Fact]
    public void Summary_Autolink_IncludesUrl()
    {
        // Arrange
        var markdown = "## T\n\nSee <https://example.com> now.";

        // Act
        var store = Load(markdown);

        // Assert
        Assert.Equal("See https://example.com now.", store.FindLesson("tools", "git")!.Summary);
    }

    [Fact]
    public void FindLesson_DifferentCase_FindsLesson()
    {
        // Arrange
        var store = Load("## T\n\ntext");

        // Act
        var found = store.FindLesson("TOOLS", "Git");

        // Assert
        Assert.NotNull(found);
        Assert.True(store.HasLesson("tools", "git"));
        Assert.False(store.HasLesson("tools", "branches"));
        Assert.Null(store.FindLesson(" ", "git"));
        Assert.Null(store.FindLesson("tools", null!));
    }

    [Fact]
    public void FromSources_DuplicateKey_ThrowsLessonStoreException()
    {
        // Arrange
        var sources = new[] { ("tools", "git", "## A\n\nx"), ("tools", "git", "## B\n\ny") };

        // Act & Assert
        Assert.Throws<LessonStoreException>(() => MarkdownLessonStore.FromSources(sources, Topics));
    }

    [Fact]
    public void LoadFromDirectory_MissingRoot_ReturnsEmptyStore()
    {
        // Arrange
        var root = Path.Combine(Path.GetTempPath(), $"lessons-missing-{Guid.NewGuid():N}");

        // Act
        var store = MarkdownLessonStore.LoadFromDirectory(root, Topics);

        // Assert
        Assert.Equal(0, store.Count);
    }

    [Fact]
    public void LoadFromDirectory_ValidTree_LoadsLessonsAndIgnoresOtherFiles()
    {
        // Arrange
        using var dir = new TempDirectory();
        dir.Write(Path.Combine("tools", "git.md"), "## T\n\nHello.");
        dir.Write(Path.Combine("tools", "notes.txt"), "ignored");
        dir.Write(".gitkeep", "");

        // Act
        var store = MarkdownLessonStore.LoadFromDirectory(dir.Path, Topics);

        // Assert
        Assert.Equal(1, store.Count);
        Assert.Equal("Hello.", store.FindLesson("tools", "git")!.Summary);
    }

    [Fact]
    public void LoadFromDirectory_NonCanonicalFileName_ThrowsLessonStoreException()
    {
        // Arrange
        using var dir = new TempDirectory();
        dir.Write(Path.Combine("tools", "Git.md"), "## T\n\ntext");

        // Act
        var ex = Assert.Throws<LessonStoreException>(() => MarkdownLessonStore.LoadFromDirectory(dir.Path, Topics));

        // Assert
        Assert.Contains("tools/Git", ex.Message);
    }

    [Fact]
    public void LoadFromDirectory_NonCanonicalDirectoryName_ThrowsLessonStoreException()
    {
        // Arrange
        using var dir = new TempDirectory();
        dir.Write(Path.Combine("Tools", "git.md"), "## T\n\ntext");

        // Act & Assert
        Assert.Throws<LessonStoreException>(() => MarkdownLessonStore.LoadFromDirectory(dir.Path, Topics));
    }

    [Theory]
    [InlineData("git.md")]
    [InlineData("tools/vcs/git.md")]
    public void LoadFromDirectory_WrongDepth_ThrowsLessonStoreException(string relativePath)
    {
        // Arrange
        using var dir = new TempDirectory();
        dir.Write(relativePath.Replace('/', Path.DirectorySeparatorChar), "## T\n\ntext");

        // Act
        var ex = Assert.Throws<LessonStoreException>(() => MarkdownLessonStore.LoadFromDirectory(dir.Path, Topics));

        // Assert
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
