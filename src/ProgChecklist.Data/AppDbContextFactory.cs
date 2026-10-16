using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ProgChecklist.Data;

/// <summary>Design-time only (<c>dotnet ef</c>): needs no real database. The connection string is a placeholder without a password.</summary>
internal sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=progchecklist;Username=postgres")
            .Options;
        return new AppDbContext(options);
    }
}
