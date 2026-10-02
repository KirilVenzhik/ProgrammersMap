namespace ProgChecklist.Core;

/// <summary>Read-only access to the topic catalog.</summary>
public interface ITopicCatalog
{
    /// <summary>Returns all sections sorted by <see cref="Section.Order"/> ascending.</summary>
    IReadOnlyList<Section> GetSections();

    /// <summary>Finds a section by slug (case-insensitive); null if not found.</summary>
    Section? FindSection(string sectionSlug);

    /// <summary>Finds a topic by section slug and topic slug (case-insensitive); null if not found.</summary>
    Topic? FindTopic(string sectionSlug, string topicSlug);
}
