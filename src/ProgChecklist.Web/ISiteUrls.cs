namespace ProgChecklist.Web;

/// <summary>Builds absolute URLs of the site.</summary>
public interface ISiteUrls
{
    /// <summary>Returns the absolute URL for a path that starts with "/".</summary>
    string Absolute(string path);
}
