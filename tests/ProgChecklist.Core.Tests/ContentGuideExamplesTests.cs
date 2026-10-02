using System.Runtime.CompilerServices;

namespace ProgChecklist.Core.Tests;

public class ContentGuideExamplesTests
{
    private const string LessonMarker = "<!-- example:lesson git/init-add-commit -->";
    private const string ResourcesMarker = "<!-- example:resources -->";

    [Fact]
    public void LessonExample_FromContentGuide_PassesLessonValidation()
    {
        // Arrange
        var markdown = ExtractBlock(LessonMarker);
        var topics = LoadTopics();

        // Act
        var store = MarkdownLessonStore.FromSources([("git", "init-add-commit", markdown)], topics);
        var lesson = store.FindLesson("git", "init-add-commit");

        // Assert
        Assert.NotNull(lesson);
        Assert.False(string.IsNullOrWhiteSpace(lesson.Summary));
        Assert.True(lesson.Summary.Length <= 160, $"Summary is {lesson.Summary.Length} chars: {lesson.Summary}");
        Assert.StartsWith("Команды git init", lesson.Summary);
        Assert.Contains("<table>", lesson.Html);
        Assert.Contains("<pre>", lesson.Html);
    }

    [Fact]
    public void ResourcesExample_FromContentGuide_PassesCatalogValidation()
    {
        // Arrange
        var json = ExtractBlock(ResourcesMarker);
        var topics = LoadTopics();

        // Act
        var catalog = JsonResourceCatalog.Parse(json, topics);
        var list = catalog.GetResources("git", "init-add-commit");

        // Assert
        Assert.Equal(2, list.Count);
        Assert.Equal("https://git-scm.com/docs/git-commit", list[0].Url.AbsoluteUri);
        Assert.True(list[1].IsFree);
        Assert.False(list[1].IsAffiliate);
        Assert.Matches("[\\u0400-\\u04FF]", list[1].Url.OriginalString);
        Assert.Equal("git-scm.com", list[1].Url.Host);
    }

    private static JsonTopicCatalog LoadTopics() =>
        JsonTopicCatalog.LoadFromFile(Path.Combine(FindRepoRoot(), "content", "topics.json"));

    private static string FindRepoRoot([CallerFilePath] string sourceFile = "")
    {
        // Artifacts may be redirected outside the repo (WSL runs, ADR-9), so fall back to this source file's location.
        foreach (var start in new[] { AppContext.BaseDirectory, Path.GetDirectoryName(sourceFile) })
        {
            for (var dir = string.IsNullOrEmpty(start) ? null : new DirectoryInfo(start); dir is not null; dir = dir.Parent)
            {
                if (File.Exists(Path.Combine(dir.FullName, "ProgChecklist.sln")))
                {
                    return dir.FullName;
                }
            }
        }

        throw new InvalidOperationException("Repository root (ProgChecklist.sln) not found above " + AppContext.BaseDirectory);
    }

    /// <summary>Returns the content of the fenced block that follows the marker line.</summary>
    private static string ExtractBlock(string marker)
    {
        var path = Path.Combine(FindRepoRoot(), "docs", "CONTENT.md");
        var lines = File.ReadAllLines(path);

        var markerIndex = Array.FindIndex(lines, l => l.Trim() == marker);
        Assert.True(markerIndex >= 0, $"Marker '{marker}' not found in {path}.");

        var openIndex = markerIndex + 1;
        while (openIndex < lines.Length && !lines[openIndex].StartsWith("```", StringComparison.Ordinal))
        {
            openIndex++;
        }

        Assert.True(openIndex < lines.Length, $"No code fence found after marker '{marker}'.");

        var open = lines[openIndex];
        var fenceLength = open.TakeWhile(c => c == '`').Count();
        var fence = new string('`', fenceLength);

        var body = new List<string>();
        for (var i = openIndex + 1; i < lines.Length; i++)
        {
            // Closing fence: same number of backticks, no info string.
            if (lines[i].TrimEnd() == fence)
            {
                return string.Join('\n', body);
            }

            body.Add(lines[i]);
        }

        throw new Xunit.Sdk.XunitException($"Closing fence '{fence}' not found after marker '{marker}'.");
    }
}
