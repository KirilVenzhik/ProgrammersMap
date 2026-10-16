using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ProgChecklist.Core;
using ProgChecklist.Data;

namespace ProgChecklist.Web.Tests;

public sealed class ProgressServiceTests : IDisposable
{
    private const string Git = "git/init-add-commit";
    private const string Join = "databases/join";
    private const string Bits = "fundamentals/binary-bits-bytes";

    private static readonly JsonTopicCatalog Catalog =
        JsonTopicCatalog.LoadFromFile(Path.Combine(AppContext.BaseDirectory, "content", "topics.json"));

    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero));

    public ProgressServiceTests()
    {
        _connection.Open();
        using var context = CreateContext();
        context.Database.EnsureCreated();
        context.Users.AddRange(NewUser("a"), NewUser("b"));
        context.SaveChanges();
    }

    public void Dispose() => _connection.Dispose();

    private AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options);

    private ProgressService CreateService(AppDbContext context) => new(context, Catalog, _clock);

    private static AppUser NewUser(string id) => new() { Id = id, UserName = id, Email = $"{id}@example.test" };

    private async Task<List<UserTopicProgress>> RowsAsync(string userId)
    {
        await using var context = CreateContext();
        return await context.TopicProgress.AsNoTracking().Where(p => p.UserId == userId).ToListAsync();
    }

    [Fact]
    public async Task Mark_KnownKey_InsertsRowWithUtcCompletedAt()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        // Act
        var result = await service.MarkAsync("a", Git);

        // Assert
        Assert.True(result);
        var row = Assert.Single(await RowsAsync("a"));
        Assert.Equal(Git, row.TopicKey);
        Assert.Equal(_clock.GetUtcNow(), row.CompletedAt);
        Assert.Equal(TimeSpan.Zero, row.CompletedAt.Offset);
    }

    [Fact]
    public async Task Mark_NonUtcClock_StoresZeroOffset()
    {
        // Arrange
        _clock.Set(new DateTimeOffset(2026, 5, 1, 12, 0, 0, TimeSpan.FromHours(2)));
        await using var context = CreateContext();

        // Act
        await CreateService(context).MarkAsync("a", Git);

        // Assert
        var row = Assert.Single(await RowsAsync("a"));
        Assert.Equal(TimeSpan.Zero, row.CompletedAt.Offset);
    }

    [Fact]
    public async Task Mark_Twice_IsNoOpAndKeepsOriginalCompletedAt()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);
        var first = _clock.GetUtcNow();
        await service.MarkAsync("a", Git);
        _clock.Advance(TimeSpan.FromHours(3));

        // Act
        var result = await service.MarkAsync("a", Git);

        // Assert
        Assert.True(result);
        var row = Assert.Single(await RowsAsync("a"));
        Assert.Equal(first, row.CompletedAt);
    }

    [Theory]
    [InlineData("git/nope")]
    [InlineData("Git/init-add-commit")]
    [InlineData("garbage")]
    [InlineData("")]
    public async Task Mark_UnknownKey_ReturnsFalseAndWritesNothing(string key)
    {
        // Arrange
        await using var context = CreateContext();

        // Act
        var result = await CreateService(context).MarkAsync("a", key);

        // Assert
        Assert.False(result);
        Assert.Empty(await RowsAsync("a"));
    }

    [Fact]
    public async Task Mark_NonExistentUser_ThrowsAndLeavesNothingTracked()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        // Act
        await Assert.ThrowsAsync<DbUpdateException>(() => service.MarkAsync("nobody", Git));

        // Assert
        Assert.Empty(context.ChangeTracker.Entries<UserTopicProgress>());
        Assert.Empty(await RowsAsync("nobody"));
    }

    [Fact]
    public async Task Mark_BlankUserId_Throws()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.MarkAsync(" ", Git));
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.GetKeysAsync(null!));
    }

    [Fact]
    public async Task Unmark_ExistingKey_RemovesRowAndRepeatIsFine()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);
        await service.MarkAsync("a", Git);
        await service.MarkAsync("a", Join);

        // Act
        var first = await service.UnmarkAsync("a", Git);
        var second = await service.UnmarkAsync("a", Git);

        // Assert
        Assert.True(first);
        Assert.True(second);
        Assert.Equal(new[] { Join }, (await RowsAsync("a")).Select(r => r.TopicKey));
    }

    [Fact]
    public async Task Unmark_UnknownKey_ReturnsFalse()
    {
        // Arrange
        await using var context = CreateContext();

        // Act
        var result = await CreateService(context).UnmarkAsync("a", "git/nope");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task Merge_WithExistingRows_ReturnsSortedUnionWithoutDuplicates()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);
        await service.MarkAsync("a", Join);

        // Act
        var result = await service.MergeAsync("a", new string?[] { Git, Join, Bits });

        // Assert
        Assert.Equal(new[] { Join, Bits, Git }.OrderBy(k => k, StringComparer.Ordinal), result);
        Assert.Equal(3, (await RowsAsync("a")).Count);
    }

    [Fact]
    public async Task Merge_DuplicatesUnknownAndNull_AreDropped()
    {
        // Arrange
        await using var context = CreateContext();

        // Act
        var result = await CreateService(context).MergeAsync(
            "a", new string?[] { Git, Git, null, "git/nope", "Git/init-add-commit", "x" });

        // Assert
        Assert.Equal(new[] { Git }, result);
        Assert.Single(await RowsAsync("a"));
    }

    [Fact]
    public async Task Merge_ExistingRows_KeepTheirCompletedAt()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);
        var first = _clock.GetUtcNow();
        await service.MarkAsync("a", Git);
        _clock.Advance(TimeSpan.FromDays(1));

        // Act
        await service.MergeAsync("a", new string?[] { Git, Join });

        // Assert
        var rows = (await RowsAsync("a")).ToDictionary(r => r.TopicKey);
        Assert.Equal(first, rows[Git].CompletedAt);
        Assert.Equal(_clock.GetUtcNow(), rows[Join].CompletedAt);
    }

    [Fact]
    public async Task Merge_ExactlyMaxKeys_Succeeds()
    {
        // Arrange
        await using var context = CreateContext();
        var keys = Enumerable.Repeat<string?>(Git, ProgressService.MaxMergeKeys).ToList();

        // Act
        var result = await CreateService(context).MergeAsync("a", keys);

        // Assert
        Assert.Equal(new[] { Git }, result);
    }

    [Fact]
    public async Task Merge_MoreThanMaxKeys_ThrowsAndWritesNothing()
    {
        // Arrange
        await using var context = CreateContext();
        var keys = Enumerable.Repeat<string?>(Git, ProgressService.MaxMergeKeys + 1).ToList();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => CreateService(context).MergeAsync("a", keys));
        Assert.Empty(await RowsAsync("a"));
    }

    [Fact]
    public async Task Clear_RemovesOnlyThatUsersRows()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);
        await service.MergeAsync("a", new string?[] { Git, Join });
        await service.MergeAsync("b", new string?[] { Git });

        // Act
        await service.ClearAsync("a");

        // Assert
        Assert.Empty(await RowsAsync("a"));
        Assert.Single(await RowsAsync("b"));
    }

    [Fact]
    public async Task GetKeys_And_Mark_AreIsolatedPerUser()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context);
        await service.MarkAsync("b", Bits);

        // Act
        await service.MarkAsync("a", Git);
        var keysA = await service.GetKeysAsync("a");
        var keysB = await service.GetKeysAsync("b");

        // Assert
        Assert.Equal(new[] { Git }, keysA);
        Assert.Equal(new[] { Bits }, keysB);
    }

    private sealed class FakeTimeProvider : TimeProvider
    {
        private DateTimeOffset _now;

        public FakeTimeProvider(DateTimeOffset now) => _now = now;

        public void Set(DateTimeOffset now) => _now = now;

        public void Advance(TimeSpan by) => _now += by;

        public override DateTimeOffset GetUtcNow() => _now;
    }
}
