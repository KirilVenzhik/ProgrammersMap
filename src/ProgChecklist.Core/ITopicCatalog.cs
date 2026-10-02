namespace ProgChecklist.Core;

/// <summary>Read-only access to the topic catalog.</summary>
public interface ITopicCatalog
{
    /// <summary>Returns all sections sorted by <see cref="Section.Order"/> ascending.</summary>
    IReadOnlyList<Section> GetSections();

    /// <summary>Total number of topics in the catalog.</summary>
    int TopicCount { get; }

    /// <summary>Finds a section by slug (case-insensitive); null if not found.</summary>
    Section? FindSection(string sectionSlug);

    /// <summary>Finds a topic by section slug and topic slug (case-insensitive); null if not found.</summary>
    Topic? FindTopic(string sectionSlug, string topicSlug);

    /// <summary>
    /// Finds a topic with its section, group and neighbours in global catalog order
    /// (case-insensitive); null if not found.
    /// </summary>
    TopicContext? FindTopicContext(string sectionSlug, string topicSlug);
}
