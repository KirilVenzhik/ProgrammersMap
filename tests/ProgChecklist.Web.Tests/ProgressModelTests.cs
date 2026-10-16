using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ProgChecklist.Data;

namespace ProgChecklist.Web.Tests;

/// <summary>Data model checks on SQLite in memory (provider-neutral model, ADR-12).</summary>
public sealed class ProgressModelTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public ProgressModelTests()
    {
        _connection.Open();
        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    public void Dispose() => _connection.Dispose();

    private AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options);

    private static AppUser NewUser(string name) => new() { UserName = name, Email = $"{name}@example.test" };

    [Fact]
    public async Task SaveProgress_ThenRead_ReturnsSameRow()
    {
        // Arrange
        var completedAt = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
        string userId;
        await using (var context = CreateContext())
        {
            var user = NewUser("alice");
            context.Users.Add(user);
            context.TopicProgress.Add(new UserTopicProgress { UserId = user.Id, TopicKey = "basics/variables", CompletedAt = completedAt });
            userId = user.Id;

            // Act
            await context.SaveChangesAsync();
        }

        // Assert
        await using var readContext = CreateContext();
        var row = await readContext.TopicProgress.SingleAsync();
        Assert.Equal(userId, row.UserId);
        Assert.Equal("basics/variables", row.TopicKey);
        Assert.Equal(completedAt, row.CompletedAt);
    }

    [Fact]
    public async Task SaveProgress_DuplicateUserAndTopic_ThrowsDbUpdateException()
    {
        // Arrange
        await using var context = CreateContext();
        var user = NewUser("bob");
        context.Users.Add(user);
        context.TopicProgress.Add(new UserTopicProgress { UserId = user.Id, TopicKey = "a/b", CompletedAt = DateTimeOffset.UtcNow });
        await context.SaveChangesAsync();

        // Act
        await using var second = CreateContext();
        second.TopicProgress.Add(new UserTopicProgress { UserId = user.Id, TopicKey = "a/b", CompletedAt = DateTimeOffset.UtcNow });

        // Assert
        await Assert.ThrowsAsync<DbUpdateException>(() => second.SaveChangesAsync());
    }

    [Fact]
    public async Task SaveProgress_SameTopicForDifferentUsers_IsAllowed()
    {
        // Arrange
        await using var context = CreateContext();
        var first = NewUser("carol");
        var second = NewUser("dave");
        context.Users.AddRange(first, second);
        context.TopicProgress.Add(new UserTopicProgress { UserId = first.Id, TopicKey = "a/b", CompletedAt = DateTimeOffset.UtcNow });
        context.TopicProgress.Add(new UserTopicProgress { UserId = second.Id, TopicKey = "a/b", CompletedAt = DateTimeOffset.UtcNow });

        // Act
        await context.SaveChangesAsync();

        // Assert
        Assert.Equal(2, await context.TopicProgress.CountAsync());
    }

    [Fact]
    public async Task SaveProgress_UnknownUser_ThrowsDbUpdateException()
    {
        // Arrange (SQLite enforces foreign keys by default in Microsoft.Data.Sqlite)
        await using var context = CreateContext();
        context.TopicProgress.Add(new UserTopicProgress { UserId = "missing", TopicKey = "a/b", CompletedAt = DateTimeOffset.UtcNow });

        // Act + Assert
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task DeleteUser_RemovesTheirProgressButNotOthers()
    {
        // Arrange
        string deletedId;
        await using (var context = CreateContext())
        {
            var doomed = NewUser("erin");
            var kept = NewUser("frank");
            context.Users.AddRange(doomed, kept);
            context.TopicProgress.Add(new UserTopicProgress { UserId = doomed.Id, TopicKey = "a/b", CompletedAt = DateTimeOffset.UtcNow });
            context.TopicProgress.Add(new UserTopicProgress { UserId = doomed.Id, TopicKey = "a/c", CompletedAt = DateTimeOffset.UtcNow });
            context.TopicProgress.Add(new UserTopicProgress { UserId = kept.Id, TopicKey = "a/b", CompletedAt = DateTimeOffset.UtcNow });
            await context.SaveChangesAsync();
            deletedId = doomed.Id;
        }

        // Act: delete without tracking progress rows, so the database cascade does the work.
        await using (var context = CreateContext())
        {
            await context.Users.Where(u => u.Id == deletedId).ExecuteDeleteAsync();
        }

        // Assert
        await using var readContext = CreateContext();
        var rows = await readContext.TopicProgress.ToListAsync();
        Assert.Single(rows);
        Assert.NotEqual(deletedId, rows[0].UserId);
    }

    [Fact]
    public void Model_TopicKey_IsRequiredWithMaxLength100()
    {
        // Arrange
        using var context = CreateContext();

        // Act
        var property = context.Model.FindEntityType(typeof(UserTopicProgress))!.FindProperty(nameof(UserTopicProgress.TopicKey))!;

        // Assert
        Assert.Equal(100, property.GetMaxLength());
        Assert.False(property.IsNullable);
    }

    [Fact]
    public void Model_UserTopicProgress_HasCompositeKeyAndCascadeForeignKey()
    {
        // Arrange
        using var context = CreateContext();

        // Act
        var entity = context.Model.FindEntityType(typeof(UserTopicProgress))!;
        var key = entity.FindPrimaryKey()!;
        var foreignKey = entity.GetForeignKeys().Single();

        // Assert
        Assert.Equal("UserTopicProgress", entity.GetTableName());
        Assert.Equal(new[] { "UserId", "TopicKey" }, key.Properties.Select(p => p.Name));
        Assert.Equal(DeleteBehavior.Cascade, foreignKey.DeleteBehavior);
    }
}
