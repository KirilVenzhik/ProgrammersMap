using ProgChecklist.Core;

namespace ProgChecklist.Web;

public static class ResourceCatalogServiceCollectionExtensions
{
    /// <summary>
    /// Loads the resource catalog eagerly (fail fast) and registers it as a singleton.
    /// Must be called after <see cref="TopicCatalogServiceCollectionExtensions.AddTopicCatalog"/>:
    /// resources are validated against the already loaded topic catalog instance.
    /// </summary>
    public static IServiceCollection AddResourceCatalog(this IServiceCollection services, IConfiguration configuration)
    {
        var path = configuration["Content:ResourcesPath"];
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException("Configuration value 'Content:ResourcesPath' is missing or empty.");
        }

        if (!Path.IsPathRooted(path))
        {
            path = Path.Combine(AppContext.BaseDirectory, path);
        }

        var topics = services
            .LastOrDefault(descriptor => descriptor.ServiceType == typeof(ITopicCatalog))
            ?.ImplementationInstance as ITopicCatalog
            ?? throw new InvalidOperationException(
                "AddResourceCatalog requires AddTopicCatalog to be called first.");

        IResourceCatalog catalog = JsonResourceCatalog.LoadFromFile(path, topics);
        return services.AddSingleton(catalog);
    }
}
