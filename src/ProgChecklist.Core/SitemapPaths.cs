namespace ProgChecklist.Core;

/// <summary>Lists all public page paths of the site.</summary>
public static class SitemapPaths
{
    /// <summary>Home, then every section, then every topic in global catalog order.</summary>
    public static IReadOnlyList<string> All(ITopicCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        var sections = catalog.GetSections();
        var paths = new List<string>(1 + sections.Count + catalog.TopicCount) { "/" };
        paths.AddRange(sections.Select(section => $"/{section.Slug}"));
        foreach (var section in sections)
        {
            foreach (var group in section.Groups)
            {
                paths.AddRange(group.Topics.Select(topic => $"/{section.Slug}/{topic.Slug}"));
            }
        }

        return paths;
    }
}
