namespace ProgChecklist.Core;

/// <summary>Read-only access to topic resources.</summary>
public interface IResourceCatalog
{
    /// <summary>
    /// Returns resources of a topic in file order (case-insensitive lookup);
    /// an empty list if there are none or the arguments are null/whitespace.
    /// </summary>
    IReadOnlyList<Resource> GetResources(string sectionSlug, string topicSlug);

    /// <summary>Number of topics that have at least one resource.</summary>
    int TopicsWithResourcesCount { get; }
}
