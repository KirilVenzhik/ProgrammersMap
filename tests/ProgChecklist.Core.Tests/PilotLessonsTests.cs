using System.Text.RegularExpressions;

namespace ProgChecklist.Core.Tests;

public class PilotLessonsTests
{
    private const int MinWords = 300;

    public static IEnumerable<object[]> Lessons() =>
    [
        ["git", "init-add-commit"],
        ["databases", "join"],
        ["fundamentals", "binary-bits-bytes"],
    ];

    [Theory]
    [MemberData(nameof(Lessons))]
    public void Lesson_HasAtLeastMinWordsOutsideCodeBlocks(string section, string topic)
    {
        // Arrange
        var path = Path.Combine(ContentGuideExamplesTests.FindRepoRoot(), "content", "lessons", section, topic + ".md");
        var inCode = false;
        var prose = new List<string>();
        foreach (var line in File.ReadAllLines(path))
        {
            if (line.TrimStart().StartsWith("```", StringComparison.Ordinal))
            {
                inCode = !inCode;
                continue;
            }

            if (!inCode)
            {
                prose.Add(line);
            }
        }

        // Act
        var words = Regex.Matches(string.Join('\n', prose), @"\S+").Count;

        // Assert
        Assert.True(words >= MinWords, $"{section}/{topic} has only {words} words outside code blocks.");
    }
}
