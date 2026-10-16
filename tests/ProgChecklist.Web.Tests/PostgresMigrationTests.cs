using Microsoft.EntityFrameworkCore;
using Npgsql;
using ProgChecklist.Data;

namespace ProgChecklist.Web.Tests;

/// <summary>
/// Runs against a real PostgreSQL server (ADR-12). Every test gets its own throwaway database.
/// </summary>
public class PostgresMigrationTests : IAsyncLifetime
{
    private readonly string _connectionString = BuildConnectionString();

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        if (string.IsNullOrEmpty(_connectionString))
        {
            return;
        }

        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
        NpgsqlConnection.ClearAllPools();
    }

    [PostgresFact]
    public async Task Migrate_EmptyDatabase_AppliesAllMigrations()
    {
        // Arrange
        await using var context = CreateContext();

        // Act
        await context.Database.MigrateAsync();

        // Assert
        var applied = (await context.Database.GetAppliedMigrationsAsync()).ToList();
        var pending = await context.Database.GetPendingMigrationsAsync();
        Assert.NotEmpty(applied);
        Assert.Equal(context.Database.GetMigrations(), applied);
        Assert.Empty(pending);
    }

    [PostgresFact]
    public async Task SaveProgress_AfterMigrate_RoundTrips()
    {
        // Arrange
        var completedAt = new DateTimeOffset(2026, 3, 4, 12, 30, 15, TimeSpan.Zero);
        await using (var setup = CreateContext())
        {
            await setup.Database.MigrateAsync();
            setup.Users.Add(NewUser("u1"));
            setup.Set<UserTopicProgress>().Add(new UserTopicProgress
            {
                UserId = "u1",
                TopicKey = "basics/variables",
                CompletedAt = completedAt,
            });

            // Act
            await setup.SaveChangesAsync();
        }

        // Assert
        await using var verify = CreateContext();
        var row = await verify.Set<UserTopicProgress>().SingleAsync();
        Assert.Equal("u1", row.UserId);
        Assert.Equal("basics/variables", row.TopicKey);
        Assert.Equal(completedAt, row.CompletedAt);
        Assert.Equal(TimeSpan.Zero, row.CompletedAt.Offset);
    }

    [PostgresFact]
    public async Task DeleteUser_AfterMigrate_CascadesProgress()
    {
        // Arrange
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        context.Users.AddRange(NewUser("u1"), NewUser("u2"));
        context.Set<UserTopicProgress>().AddRange(
            Progress("u1", "a/one"),
            Progress("u1", "a/two"),
            Progress("u2", "a/one"));
        await context.SaveChangesAsync();

        // Act
        await context.Users.Where(u => u.Id == "u1").ExecuteDeleteAsync();

        // Assert
        var remaining = await context.Set<UserTopicProgress>().AsNoTracking().ToListAsync();
        var row = Assert.Single(remaining);
        Assert.Equal("u2", row.UserId);
    }

    [PostgresFact]
    public async Task SaveProgress_TopicKeyLongerThanLimit_ThrowsDbUpdateException()
    {
        // Arrange
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        context.Users.Add(NewUser("u1"));
        context.Set<UserTopicProgress>().Add(
            Progress("u1", new string('k', UserTopicProgress.TopicKeyMaxLength + 1)));

        // Act
        var save = () => context.SaveChangesAsync();

        // Assert
        await Assert.ThrowsAsync<DbUpdateException>(save);
    }

    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(_connectionString).Options;
        return new AppDbContext(options);
    }

    private static AppUser NewUser(string id) => new()
    {
        Id = id,
        UserName = id,
        NormalizedUserName = id.ToUpperInvariant(),
        CreatedAt = DateTimeOffset.UtcNow,
    };

    private static UserTopicProgress Progress(string userId, string topicKey) => new()
    {
        UserId = userId,
        TopicKey = topicKey,
        CompletedAt = DateTimeOffset.UtcNow,
    };

    private static string BuildConnectionString()
    {
        var value = Environment.GetEnvironmentVariable(PostgresFactAttribute.EnvironmentVariable);
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var builder = new NpgsqlConnectionStringBuilder(value)
        {
            Database = $"progchecklist_test_{Guid.NewGuid():N}",
            Pooling = false,
        };
        return builder.ConnectionString;
    }
}
