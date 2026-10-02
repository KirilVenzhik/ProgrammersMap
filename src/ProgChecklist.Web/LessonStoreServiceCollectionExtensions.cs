using ProgChecklist.Core;

namespace ProgChecklist.Web;

public static class LessonStoreServiceCollectionExtensions
{
    /// <summary>
    /// Loads lessons eagerly (fail fast) and registers the store as a singleton.
    /// Must be called after <see cref="TopicCatalogServiceCollectionExtensions.AddTopicCatalog"/>:
    /// lessons are validated against the already loaded topic catalog instance.
    /// </summary>
    public static IServiceCollection AddLessonStore(this IServiceCollection services, IConfiguration configuration)
    {
        var path = configuration["Content:LessonsPath"];
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException("Configuration value 'Content:LessonsPath' is missing or empty.");
        }

        if (!Path.IsPathRooted(path))
        {
            path = Path.Combine(AppContext.BaseDirectory, path);
        }

        var topics = services
            .LastOrDefault(descriptor => descriptor.ServiceType == typeof(ITopicCatalog))
            ?.ImplementationInstance as ITopicCatalog
            ?? throw new InvalidOperationException(
                "AddLessonStore requires AddTopicCatalog to be called first.");

        ILessonStore store = MarkdownLessonStore.LoadFromDirectory(path, topics);
        return services.AddSingleton(store);
    }
}
