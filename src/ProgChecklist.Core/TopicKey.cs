namespace ProgChecklist.Core;

/// <summary>Builds and validates the progress key of a topic ("{section}/{topic}").</summary>
public static class TopicKey
{
    /// <summary>Returns "{sectionSlug}/{topicSlug}".</summary>
    public static string Create(string sectionSlug, string topicSlug)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionSlug);
        ArgumentException.ThrowIfNullOrWhiteSpace(topicSlug);
        return $"{sectionSlug}/{topicSlug}";
    }

    /// <summary>
    /// Parses a key of the exact form "{section}/{topic}": one slash, both parts non-empty,
    /// no whitespace anywhere.
    /// </summary>
    public static bool TryParse(string? key, out string sectionSlug, out string topicSlug)
    {
        sectionSlug = string.Empty;
        topicSlug = string.Empty;
        if (string.IsNullOrEmpty(key))
        {
            return false;
        }

        var slash = key.IndexOf('/');
        if (slash <= 0 || slash == key.Length - 1 || key.IndexOf('/', slash + 1) >= 0)
        {
            return false;
        }

        foreach (var c in key)
        {
            if (char.IsWhiteSpace(c))
            {
                return false;
            }
        }

        sectionSlug = key[..slash];
        topicSlug = key[(slash + 1)..];
        return true;
    }

    /// <summary>True when the key is well-formed and names a topic of the catalog (ordinal, case-sensitive).</summary>
    public static bool IsKnown(ITopicCatalog catalog, string? key)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        if (!TryParse(key, out var sectionSlug, out var topicSlug))
        {
            return false;
        }

        var topic = catalog.FindTopic(sectionSlug, topicSlug);
        if (topic is null || !string.Equals(topic.Slug, topicSlug, StringComparison.Ordinal))
        {
            return false;
        }

        var section = catalog.FindSection(sectionSlug);
        return section is not null && string.Equals(section.Slug, sectionSlug, StringComparison.Ordinal);
    }

    /// <summary>Keeps only known keys, drops duplicates (ordinal), preserves first-occurrence order.</summary>
    public static IReadOnlyList<string> Normalize(ITopicCatalog catalog, IEnumerable<string?> keys)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(keys);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<string>();
        foreach (var key in keys)
        {
            if (IsKnown(catalog, key) && seen.Add(key!))
            {
                result.Add(key!);
            }
        }

        return result;
    }
}
