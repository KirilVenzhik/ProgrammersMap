namespace ProgChecklist.Web;

/// <summary>Site-wide settings bound from the "Site" configuration section.</summary>
public sealed class SiteOptions
{
    /// <summary>Public base URL, e.g. https://example.ru. Empty means "use the current request".</summary>
    public string? BaseUrl { get; set; }
}
