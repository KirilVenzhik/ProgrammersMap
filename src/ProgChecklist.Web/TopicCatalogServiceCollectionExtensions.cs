using ProgChecklist.Core;

namespace ProgChecklist.Web;

public static class TopicCatalogServiceCollectionExtensions
{
    /// <summary>Loads the topic catalog eagerly (fail fast) and registers it as a singleton.</summary>
    public static IServiceCollection AddTopicCatalog(this IServiceCollection services, IConfiguration configuration)
    {
        var path = configuration["Content:TopicsPath"];
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException("Configuration value 'Content:TopicsPath' is missing or empty.");
        }

        if (!Path.IsPathRooted(path))
        {
            path = Path.Combine(AppContext.BaseDirectory, path);
        }

        ITopicCatalog catalog = JsonTopicCatalog.LoadFromFile(path);
        return services.AddSingleton(catalog);
    }
}
