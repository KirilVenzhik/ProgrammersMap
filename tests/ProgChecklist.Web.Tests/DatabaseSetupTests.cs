using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using ProgChecklist.Data;

namespace ProgChecklist.Web.Tests;

public class DatabaseSetupTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Startup_WithoutConnectionString_FailsWithClearMessage(string value)
    {
        // Arrange
        using var factory = new MissingConnectionStringFactory(value);

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());

        // Assert
        Assert.Contains("ConnectionStrings:Default", exception.Message);
    }

    [Fact]
    public void Migrations_MatchModel_NoPendingChanges()
    {
        // Arrange (no connection is opened by HasPendingModelChanges)
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=progchecklist;Username=postgres")
            .Options;
        using var context = new AppDbContext(options);

        // Act
        var hasPending = context.Database.HasPendingModelChanges();

        // Assert
        Assert.False(hasPending);
    }

    /// <summary>Plain factory without the SQLite override: only the fail-fast check is exercised.</summary>
    private sealed class MissingConnectionStringFactory(string value) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder) =>
            builder.UseSetting("ConnectionStrings:Default", value);
    }
}
