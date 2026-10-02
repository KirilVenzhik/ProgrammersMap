using Microsoft.Extensions.Options;

namespace ProgChecklist.Web;

/// <summary>
/// Uses the configured base URL when set (see ADR-8); otherwise falls back to the current request.
/// </summary>
public sealed class SiteUrls : ISiteUrls
{
    private readonly SiteOptions _options;
    private readonly IHttpContextAccessor _accessor;

    public SiteUrls(IOptions<SiteOptions> options, IHttpContextAccessor accessor)
    {
        _options = options.Value;
        _accessor = accessor;
    }

    public string Absolute(string path)
    {
        if (string.IsNullOrEmpty(path) || path[0] != '/')
        {
            throw new ArgumentException("Path must start with '/'.", nameof(path));
        }

        if (!string.IsNullOrEmpty(_options.BaseUrl))
        {
            return _options.BaseUrl + path;
        }

        var request = _accessor.HttpContext?.Request
            ?? throw new InvalidOperationException("No Site:BaseUrl configured and no current request.");
        return $"{request.Scheme}://{request.Host}{path}";
    }
}
