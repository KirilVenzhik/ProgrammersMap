using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProgChecklist.Core;

/// <summary>Resource catalog loaded from a JSON document and validated against the topic catalog.</summary>
public sealed class JsonResourceCatalog : IResourceCatalog
{
    private const int SupportedVersion = 1;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    private static readonly IReadOnlyList<Resource> NoResources = Array.AsReadOnly(Array.Empty<Resource>());

    private readonly Dictionary<string, IReadOnlyList<Resource>> resourcesByKey;

    private JsonResourceCatalog(Dictionary<string, IReadOnlyList<Resource>> resourcesByKey)
    {
        this.resourcesByKey = resourcesByKey;
    }

    /// <inheritdoc />
    public int TopicsWithResourcesCount => resourcesByKey.Count;

    /// <summary>Loads the catalog from a UTF-8 JSON file.</summary>
    public static JsonResourceCatalog LoadFromFile(string path, ITopicCatalog topics)
    {
        string json;
        try
        {
            json = File.ReadAllText(path, Encoding.UTF8);
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            throw new ResourceCatalogException($"Resource catalog file not found: '{path}'.", ex);
        }

        return Parse(json, topics);
    }

    /// <summary>Parses and validates the catalog from a JSON string.</summary>
    public static JsonResourceCatalog Parse(string json, ITopicCatalog topics)
    {
        ArgumentNullException.ThrowIfNull(topics);

        CatalogDto? root;
        try
        {
            root = JsonSerializer.Deserialize<CatalogDto>(json, SerializerOptions);
        }
        catch (JsonException ex)
        {
            throw new ResourceCatalogException($"Resource catalog JSON is invalid: {ex.Message}", ex);
        }

        if (root is null)
        {
            throw new ResourceCatalogException("Resource catalog JSON root is null.");
        }

        if (root.Version != SupportedVersion)
        {
            throw new ResourceCatalogException(
                $"Unsupported resource catalog version '{root.Version?.ToString() ?? "(missing)"}'. Expected {SupportedVersion}.");
        }

        if (root.Resources is null)
        {
            throw new ResourceCatalogException("Resource catalog has no 'resources' object.");
        }

        var result = new Dictionary<string, IReadOnlyList<Resource>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, items) in root.Resources)
        {
            ValidateKey(key, topics);

            if (items is null || items.Count == 0)
            {
                throw new ResourceCatalogException(
                    $"Topic '{key}' has an empty resource list. Omit the key instead.");
            }

            var seenUrls = new HashSet<Uri>();
            var list = new List<Resource>(items.Count);
            foreach (var item in items)
            {
                if (item is null)
                {
                    throw new ResourceCatalogException($"Topic '{key}' contains a null resource.");
                }

                var title = item.Title;
                if (string.IsNullOrWhiteSpace(title))
                {
                    throw new ResourceCatalogException($"Missing resource title in topic '{key}'.");
                }

                var url = ParseUrl(item.Url, key);
                if (!seenUrls.Add(url))
                {
                    throw new ResourceCatalogException($"Duplicate resource url '{url}' in topic '{key}'.");
                }

                list.Add(new Resource(
                    title,
                    url,
                    ParseKind(item.Kind, key),
                    ParseLanguage(item.Lang, key),
                    item.Free ?? true,
                    item.Affiliate ?? false));
            }

            result.Add(key, list.AsReadOnly());
        }

        return new JsonResourceCatalog(result);
    }

    /// <inheritdoc />
    public IReadOnlyList<Resource> GetResources(string sectionSlug, string topicSlug)
    {
        if (string.IsNullOrWhiteSpace(sectionSlug) || string.IsNullOrWhiteSpace(topicSlug))
        {
            return NoResources;
        }

        return resourcesByKey.GetValueOrDefault(TopicKey.Create(sectionSlug, topicSlug)) ?? NoResources;
    }

    private static void ValidateKey(string key, ITopicCatalog topics)
    {
        var parts = key.Split('/');
        if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
        {
            throw new ResourceCatalogException(
                $"Invalid topic key '{key}'. Expected '{{section}}/{{topic}}'.");
        }

        var context = topics.FindTopicContext(parts[0], parts[1]);
        if (context is null)
        {
            throw new ResourceCatalogException($"Unknown topic '{key}' in resource catalog.");
        }

        var canonical = TopicKey.Create(context.Section.Slug, context.Topic.Slug);
        if (!string.Equals(key, canonical, StringComparison.Ordinal))
        {
            throw new ResourceCatalogException(
                $"Topic key '{key}' is not canonical. Use '{canonical}'.");
        }
    }

    private static Uri ParseUrl(string? value, string key)
    {
        if (string.IsNullOrWhiteSpace(value)
            || !Uri.TryCreate(value, UriKind.Absolute, out var url)
            || url.Scheme != Uri.UriSchemeHttps)
        {
            throw new ResourceCatalogException(
                $"Invalid resource url '{value}' in topic '{key}'. Expected an absolute https URL.");
        }

        return url;
    }

    private static ResourceKind ParseKind(string? value, string key)
    {
        var text = value?.Trim();
        if (text is not null)
        {
            foreach (var kind in Enum.GetValues<ResourceKind>())
            {
                if (string.Equals(text, kind.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    return kind;
                }
            }
        }

        throw new ResourceCatalogException(
            $"Unknown resource kind '{value}' in topic '{key}'. Expected docs, article, video, course, book or practice.");
    }

    private static ResourceLanguage ParseLanguage(string? value, string key)
    {
        var text = value?.Trim();
        if (string.Equals(text, "ru", StringComparison.OrdinalIgnoreCase))
        {
            return ResourceLanguage.Russian;
        }

        if (string.Equals(text, "en", StringComparison.OrdinalIgnoreCase))
        {
            return ResourceLanguage.English;
        }

        throw new ResourceCatalogException(
            $"Unknown resource language '{value}' in topic '{key}'. Expected ru or en.");
    }

    private sealed class CatalogDto
    {
        public int? Version { get; set; }

        public Dictionary<string, List<ResourceDto?>?>? Resources { get; set; }
    }

    private sealed class ResourceDto
    {
        public string? Title { get; set; }

        public string? Url { get; set; }

        public string? Kind { get; set; }

        public string? Lang { get; set; }

        public bool? Free { get; set; }

        public bool? Affiliate { get; set; }
    }
}
