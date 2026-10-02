namespace ProgChecklist.Core;

/// <summary>Builds the progress key of a topic (localStorage key format).</summary>
public static class TopicKey
{
    /// <summary>Returns "{sectionSlug}/{topicSlug}".</summary>
    public static string Create(string sectionSlug, string topicSlug)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionSlug);
        ArgumentException.ThrowIfNullOrWhiteSpace(topicSlug);
        return $"{sectionSlug}/{topicSlug}";
    }
}
