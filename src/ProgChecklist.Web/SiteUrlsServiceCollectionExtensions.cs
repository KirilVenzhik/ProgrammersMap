namespace ProgChecklist.Web;

public static class SiteUrlsServiceCollectionExtensions
{
    /// <summary>Validates "Site:BaseUrl" (fail fast), normalizes it and registers <see cref="ISiteUrls"/>.</summary>
    public static IServiceCollection AddSiteUrls(this IServiceCollection services, IConfiguration configuration)
    {
        var baseUrl = Normalize(configuration["Site:BaseUrl"]);
        services.Configure<SiteOptions>(options => options.BaseUrl = baseUrl);
        services.AddHttpContextAccessor();
        return services.AddScoped<ISiteUrls, SiteUrls>();
    }

    private static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            || uri.AbsolutePath != "/"
            || !string.IsNullOrEmpty(uri.UserInfo)
            || trimmed.Contains('?')
            || trimmed.Contains('#'))
        {
            throw new InvalidOperationException(
                "Configuration value 'Site:BaseUrl' must be an absolute http/https URL without path, query or fragment.");
        }

        return trimmed.TrimEnd('/');
    }
}
