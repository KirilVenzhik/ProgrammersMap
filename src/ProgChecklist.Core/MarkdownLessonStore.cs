using System.Text;
using Markdig;
using Markdig.Extensions.AutoIdentifiers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace ProgChecklist.Core;

/// <summary>Lessons loaded from Markdown files, validated against the topic catalog and rendered once.</summary>
public sealed class MarkdownLessonStore : ILessonStore
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UsePipeTables()
        .UseEmphasisExtras()
        .UseTaskLists()
        .UseAutoIdentifiers(AutoIdentifierOptions.GitHub)
        .DisableHtml()
        .Build();

    private static readonly string[] AllowedSchemes = ["http", "https", "mailto"];

    private readonly Dictionary<string, Lesson> lessonsByKey;

    private MarkdownLessonStore(Dictionary<string, Lesson> lessonsByKey)
    {
        this.lessonsByKey = lessonsByKey;
    }

    /// <inheritdoc />
    public int Count => lessonsByKey.Count;

    /// <summary>Loads lessons from <c>root/{sectionSlug}/{topicSlug}.md</c>. A missing root means no lessons.</summary>
    public static MarkdownLessonStore LoadFromDirectory(string root, ITopicCatalog topics)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(topics);

        var sources = new List<(string SectionSlug, string TopicSlug, string Markdown)>();
        if (Directory.Exists(root))
        {
            foreach (var file in Directory.EnumerateFiles(root, "*.md", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(root, file);
                var parts = relative.Split(
                    [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
                    StringSplitOptions.None);
                if (parts.Length != 2)
                {
                    throw new LessonStoreException(
                        $"Lesson file '{relative}' must be located at '{{section}}/{{topic}}.md'.");
                }

                var topicSlug = Path.GetFileNameWithoutExtension(parts[1]);
                string markdown;
                try
                {
                    markdown = File.ReadAllText(file, Encoding.UTF8);
                }
                catch (IOException ex)
                {
                    throw new LessonStoreException($"Cannot read lesson file '{relative}': {ex.Message}", ex);
                }

                sources.Add((parts[0], topicSlug, markdown));
            }
        }

        return FromSources(sources, topics);
    }

    /// <summary>Validates and renders lessons from in-memory sources.</summary>
    public static MarkdownLessonStore FromSources(
        IEnumerable<(string SectionSlug, string TopicSlug, string Markdown)> sources,
        ITopicCatalog topics)
    {
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(topics);

        var result = new Dictionary<string, Lesson>(StringComparer.OrdinalIgnoreCase);
        foreach (var (sectionSlug, topicSlug, markdown) in sources)
        {
            var key = $"{sectionSlug}/{topicSlug}";
            var context = topics.FindTopicContext(sectionSlug, topicSlug)
                ?? throw new LessonStoreException($"Lesson '{key}' has no matching topic in the catalog.");

            if (!string.Equals(context.Section.Slug, sectionSlug, StringComparison.Ordinal)
                || !string.Equals(context.Topic.Slug, topicSlug, StringComparison.Ordinal))
            {
                throw new LessonStoreException(
                    $"Lesson '{key}' is not canonical. Use '{TopicKey.Create(context.Section.Slug, context.Topic.Slug)}'.");
            }

            if (string.IsNullOrWhiteSpace(markdown))
            {
                throw new LessonStoreException($"Lesson '{key}' is empty.");
            }

            var document = Markdown.Parse(markdown, Pipeline);
            Validate(document, key);

            var html = document.ToHtml(Pipeline);
            var summary = ExtractSummary(document);
            if (!result.TryAdd(key, new Lesson(sectionSlug, topicSlug, html, summary)))
            {
                throw new LessonStoreException($"Duplicate lesson '{key}'.");
            }
        }

        return new MarkdownLessonStore(result);
    }

    /// <inheritdoc />
    public Lesson? FindLesson(string sectionSlug, string topicSlug)
    {
        if (string.IsNullOrWhiteSpace(sectionSlug) || string.IsNullOrWhiteSpace(topicSlug))
        {
            return null;
        }

        return lessonsByKey.GetValueOrDefault(TopicKey.Create(sectionSlug, topicSlug));
    }

    /// <inheritdoc />
    public bool HasLesson(string sectionSlug, string topicSlug) => FindLesson(sectionSlug, topicSlug) is not null;

    private static void Validate(MarkdownDocument document, string key)
    {
        foreach (var heading in document.Descendants<HeadingBlock>())
        {
            if (heading.Level == 1)
            {
                throw new LessonStoreException(
                    $"Lesson '{key}' contains a level-1 heading. Lessons must start at '##'.");
            }
        }

        foreach (var link in document.Descendants<LinkInline>())
        {
            var scheme = GetScheme(link.Url);
            if (scheme is not null && !AllowedSchemes.Contains(scheme, StringComparer.Ordinal))
            {
                throw new LessonStoreException(
                    $"Lesson '{key}' contains a {(link.IsImage ? "image" : "link")} with a disallowed scheme '{scheme}:'.");
            }
        }

        foreach (var autolink in document.Descendants<AutolinkInline>())
        {
            var scheme = GetScheme(autolink.Url);
            if (scheme is not null && !AllowedSchemes.Contains(scheme, StringComparer.Ordinal))
            {
                throw new LessonStoreException(
                    $"Lesson '{key}' contains a link with a disallowed scheme '{scheme}:'.");
            }
        }
    }

    /// <summary>Returns the lower-case URL scheme, or null for relative URLs and anchors.</summary>
    private static string? GetScheme(string? url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return null;
        }

        // Browsers ignore control characters and whitespace inside the scheme, so drop them first.
        var cleaned = new string(url.Where(c => !char.IsControl(c) && !char.IsWhiteSpace(c)).ToArray());
        var colon = cleaned.IndexOf(':');
        if (colon <= 0)
        {
            return null;
        }

        var candidate = cleaned[..colon];
        if (!char.IsAsciiLetter(candidate[0])
            || !candidate.All(c => char.IsAsciiLetterOrDigit(c) || c is '+' or '-' or '.'))
        {
            return null;
        }

        return candidate.ToLowerInvariant();
    }

    private static string ExtractSummary(MarkdownDocument document)
    {
        var paragraph = document.Descendants<ParagraphBlock>().FirstOrDefault();
        if (paragraph?.Inline is null)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        AppendPlainText(paragraph.Inline, builder);
        return string.Join(' ', builder.ToString().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    private static void AppendPlainText(Inline inline, StringBuilder builder)
    {
        switch (inline)
        {
            case LiteralInline literal:
                builder.Append(literal.Content.ToString());
                break;
            case CodeInline code:
                builder.Append(code.Content);
                break;
            case LineBreakInline:
                builder.Append(' ');
                break;
            case ContainerInline container:
                for (var child = container.FirstChild; child is not null; child = child.NextSibling)
                {
                    AppendPlainText(child, builder);
                }

                break;
        }
    }
}
