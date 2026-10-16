using Microsoft.EntityFrameworkCore;
using ProgChecklist.Data;

namespace ProgChecklist.Web;

public static class DatabaseServiceCollectionExtensions
{
    /// <summary>Registers <see cref="AppDbContext"/> on PostgreSQL; fails fast when the connection string is missing.</summary>
    public static IServiceCollection AddAppDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration["ConnectionStrings:Default"];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Configuration value 'ConnectionStrings:Default' is missing or empty. " +
                "Set it via user-secrets ('dotnet user-secrets set ConnectionStrings:Default ...') " +
                "or the environment variable ConnectionStrings__Default.");
        }

        return services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
    }
}
