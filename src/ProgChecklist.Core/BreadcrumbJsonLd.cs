using System.Text.Json;

namespace ProgChecklist.Core;

/// <summary>Builds the schema.org BreadcrumbList JSON-LD of a topic page.</summary>
public static class BreadcrumbJsonLd
{
    /// <summary>Returns the serialized JSON; <paramref name="absoluteUrl"/> maps a path ("/x") to an absolute URL.</summary>
    /// <remarks>
    /// Default JsonSerializer options keep the default encoder, which escapes &lt;, &gt; and &amp;,
    /// so the result is safe to embed inside a script element.
    /// </remarks>
    public static string Build(TopicContext context, Func<string, string> absoluteUrl)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(absoluteUrl);
        var sectionPath = $"/{context.Section.Slug}";
        var data = new Dictionary<string, object>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "BreadcrumbList",
            ["itemListElement"] = new[]
            {
                ListItem(1, "Карта программиста", absoluteUrl("/")),
                ListItem(2, context.Section.Title, absoluteUrl(sectionPath)),
                ListItem(3, context.Topic.Title, absoluteUrl($"{sectionPath}/{context.Topic.Slug}")),
            },
        };
        return JsonSerializer.Serialize(data);
    }

    private static Dictionary<string, object> ListItem(int position, string name, string url) => new()
    {
        ["@type"] = "ListItem",
        ["position"] = position,
        ["name"] = name,
        ["item"] = url,
    };
}
