using System.Text;
using System.Text.Json;

namespace ProgChecklist.Core;

/// <summary>Topic catalog loaded from a JSON document.</summary>
public sealed class JsonTopicCatalog : ITopicCatalog
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IReadOnlyList<Section> sections;
    private readonly Dictionary<string, Section> sectionsBySlug;
    private readonly Dictionary<string, Dictionary<string, Topic>> topicsBySection;

    private JsonTopicCatalog(
        IReadOnlyList<Section> sections,
        Dictionary<string, Section> sectionsBySlug,
        Dictionary<string, Dictionary<string, Topic>> topicsBySection)
    {
        this.sections = sections;
        this.sectionsBySlug = sectionsBySlug;
        this.topicsBySection = topicsBySection;
        TopicCount = sections.Sum(section => section.TopicCount);
    }

    /// <summary>Loads the catalog from a UTF-8 JSON file.</summary>
    public static JsonTopicCatalog LoadFromFile(string path)
    {
        string json;
        try
        {
            json = File.ReadAllText(path, Encoding.UTF8);
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            throw new TopicCatalogException($"Topic catalog file not found: '{path}'.", ex);
        }

        return Parse(json);
    }

    /// <summary>Parses and validates the catalog from a JSON string.</summary>
    public static JsonTopicCatalog Parse(string json)
    {
        CatalogDto? root;
        try
        {
            root = JsonSerializer.Deserialize<CatalogDto>(json, SerializerOptions);
        }
        catch (JsonException ex)
        {
            throw new TopicCatalogException($"Topic catalog JSON is invalid: {ex.Message}", ex);
        }

        if (root is null)
        {
            throw new TopicCatalogException("Topic catalog JSON root is null.");
        }

        if (root.Sections is null || root.Sections.Count == 0)
        {
            throw new TopicCatalogException("Topic catalog has no sections.");
        }

        var sectionsBySlug = new Dictionary<string, Section>(StringComparer.OrdinalIgnoreCase);
        var topicsBySection = new Dictionary<string, Dictionary<string, Topic>>(StringComparer.OrdinalIgnoreCase);
        var sectionList = new List<Section>();

        foreach (var sectionDto in root.Sections)
        {
            if (sectionDto is null)
            {
                throw new TopicCatalogException("Topic catalog contains a null section.");
            }

            var sectionSlug = RequireText(sectionDto.Slug, "section slug", "(unknown)");
            var sectionTitle = RequireText(sectionDto.Title, "section title", sectionSlug);
            if (sectionsBySlug.ContainsKey(sectionSlug))
            {
                throw new TopicCatalogException($"Duplicate section slug '{sectionSlug}'.");
            }

            var groupSlugs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var topics = new Dictionary<string, Topic>(StringComparer.OrdinalIgnoreCase);
            var groups = new List<Group>();

            foreach (var groupDto in sectionDto.Groups ?? [])
            {
                if (groupDto is null)
                {
                    throw new TopicCatalogException($"Section '{sectionSlug}' contains a null group.");
                }

                var groupSlug = RequireText(groupDto.Slug, "group slug", sectionSlug);
                var groupTitle = RequireText(groupDto.Title, "group title", $"{sectionSlug}/{groupSlug}");
                if (!groupSlugs.Add(groupSlug))
                {
                    throw new TopicCatalogException($"Duplicate group slug '{groupSlug}' in section '{sectionSlug}'.");
                }

                var groupTopics = new List<Topic>();
                foreach (var topicDto in groupDto.Topics ?? [])
                {
                    if (topicDto is null)
                    {
                        throw new TopicCatalogException($"Group '{sectionSlug}/{groupSlug}' contains a null topic.");
                    }

                    var topicSlug = RequireText(topicDto.Slug, "topic slug", $"{sectionSlug}/{groupSlug}");
                    var topicKey = $"{sectionSlug}/{topicSlug}";
                    var topicTitle = RequireText(topicDto.Title, "topic title", topicKey);
                    var level = ParseLevel(topicDto.Level, topicKey);

                    var topic = new Topic(topicSlug, topicTitle, level);
                    if (!topics.TryAdd(topicSlug, topic))
                    {
                        throw new TopicCatalogException($"Duplicate topic slug '{topicKey}'.");
                    }

                    groupTopics.Add(topic);
                }

                groups.Add(new Group(groupSlug, groupTitle, groupTopics.AsReadOnly()));
            }

            var section = new Section(sectionSlug, sectionTitle, sectionDto.Order, groups.AsReadOnly());
            sectionsBySlug.Add(sectionSlug, section);
            topicsBySection.Add(sectionSlug, topics);
            sectionList.Add(section);
        }

        var sorted = sectionList.OrderBy(section => section.Order).ToList().AsReadOnly();
        return new JsonTopicCatalog(sorted, sectionsBySlug, topicsBySection);
    }

    /// <inheritdoc />
    public int TopicCount { get; }

    /// <inheritdoc />
    public IReadOnlyList<Section> GetSections() => sections;

    /// <inheritdoc />
    public Section? FindSection(string sectionSlug)
    {
        if (string.IsNullOrWhiteSpace(sectionSlug))
        {
            return null;
        }

        return sectionsBySlug.GetValueOrDefault(sectionSlug);
    }

    /// <inheritdoc />
    public Topic? FindTopic(string sectionSlug, string topicSlug)
    {
        if (string.IsNullOrWhiteSpace(sectionSlug) || string.IsNullOrWhiteSpace(topicSlug))
        {
            return null;
        }

        return topicsBySection.TryGetValue(sectionSlug, out var topics)
            ? topics.GetValueOrDefault(topicSlug)
            : null;
    }

    private static string RequireText(string? value, string what, string context)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new TopicCatalogException($"Missing {what} (context: {context}).");
        }

        return value;
    }

    private static Level ParseLevel(string? value, string topicKey)
    {
        var text = value?.Trim();
        if (text is not null)
        {
            if (string.Equals(text, "junior", StringComparison.OrdinalIgnoreCase))
            {
                return Level.Junior;
            }

            if (string.Equals(text, "middle", StringComparison.OrdinalIgnoreCase))
            {
                return Level.Middle;
            }

            if (string.Equals(text, "senior", StringComparison.OrdinalIgnoreCase))
            {
                return Level.Senior;
            }
        }

        throw new TopicCatalogException(
            $"Unknown level '{value}' for topic '{topicKey}'. Expected junior, middle or senior.");
    }

    private sealed class CatalogDto
    {
        public List<SectionDto?>? Sections { get; set; }
    }

    private sealed class SectionDto
    {
        public string? Slug { get; set; }

        public string? Title { get; set; }

        public int Order { get; set; }

        public List<GroupDto?>? Groups { get; set; }
    }

    private sealed class GroupDto
    {
        public string? Slug { get; set; }

        public string? Title { get; set; }

        public List<TopicDto?>? Topics { get; set; }
    }

    private sealed class TopicDto
    {
        public string? Slug { get; set; }

        public string? Title { get; set; }

        public string? Level { get; set; }
    }
}
