using Microsoft.EntityFrameworkCore;
using ProgChecklist.Core;

namespace ProgChecklist.Data;

public sealed class ProgressService : IProgressService
{
    /// <summary>Maximum number of raw keys accepted by <see cref="MergeAsync"/>.</summary>
    public const int MaxMergeKeys = 1000;

    private readonly AppDbContext _db;
    private readonly ITopicCatalog _catalog;
    private readonly TimeProvider _time;

    public ProgressService(AppDbContext db, ITopicCatalog catalog, TimeProvider time)
    {
        _db = db;
        _catalog = catalog;
        _time = time;
    }

    public async Task<IReadOnlyList<string>> GetKeysAsync(string userId, CancellationToken ct = default)
    {
        ValidateUser(userId);
        var keys = await _db.TopicProgress.AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => p.TopicKey)
            .ToListAsync(ct);
        keys.Sort(StringComparer.Ordinal);
        return keys;
    }

    public async Task<IReadOnlyList<ProgressEntry>> GetEntriesAsync(string userId, CancellationToken ct = default)
    {
        ValidateUser(userId);
        var entries = await _db.TopicProgress.AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => new ProgressEntry(p.TopicKey, p.CompletedAt))
            .ToListAsync(ct);
        entries.Sort((a, b) => string.CompareOrdinal(a.TopicKey, b.TopicKey));
        return entries;
    }

    public async Task<bool> MarkAsync(string userId, string topicKey, CancellationToken ct = default)
    {
        ValidateUser(userId);
        if (!TopicKey.IsKnown(_catalog, topicKey))
        {
            return false;
        }

        await InsertMissingAsync(userId, [topicKey], ct);
        return true;
    }

    public async Task<bool> UnmarkAsync(string userId, string topicKey, CancellationToken ct = default)
    {
        ValidateUser(userId);
        if (!TopicKey.IsKnown(_catalog, topicKey))
        {
            return false;
        }

        await _db.TopicProgress.Where(p => p.UserId == userId && p.TopicKey == topicKey).ExecuteDeleteAsync(ct);
        return true;
    }

    public async Task ClearAsync(string userId, CancellationToken ct = default)
    {
        ValidateUser(userId);
        await _db.TopicProgress.Where(p => p.UserId == userId).ExecuteDeleteAsync(ct);
    }

    public async Task<IReadOnlyList<string>> MergeAsync(
        string userId, IReadOnlyCollection<string?> keys, CancellationToken ct = default)
    {
        ValidateUser(userId);
        ArgumentNullException.ThrowIfNull(keys);
        if (keys.Count > MaxMergeKeys)
        {
            throw new ArgumentException($"At most {MaxMergeKeys} keys can be merged at once.", nameof(keys));
        }

        var normalized = TopicKey.Normalize(_catalog, keys);
        await InsertMissingAsync(userId, normalized, ct);
        return await GetKeysAsync(userId, ct);
    }

    private async Task InsertMissingAsync(string userId, IReadOnlyList<string> keys, CancellationToken ct)
    {
        if (keys.Count == 0)
        {
            return;
        }

        // The second attempt covers a concurrent insert of the same key: it reloads existing rows.
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var existing = await _db.TopicProgress.AsNoTracking()
                .Where(p => p.UserId == userId && keys.Contains(p.TopicKey))
                .Select(p => p.TopicKey)
                .ToListAsync(ct);
            var missing = keys.Except(existing, StringComparer.Ordinal).ToList();
            if (missing.Count == 0)
            {
                return;
            }

            var now = _time.GetUtcNow().ToUniversalTime();
            var added = missing.Select(key => new UserTopicProgress
            {
                UserId = userId,
                TopicKey = key,
                CompletedAt = now,
            }).ToList();
            _db.TopicProgress.AddRange(added);
            try
            {
                await _db.SaveChangesAsync(ct);
                return;
            }
            catch (DbUpdateException)
            {
                // Generic one-shot retry; never leave failed rows tracked in the scoped context.
                foreach (var row in added)
                {
                    _db.Entry(row).State = EntityState.Detached;
                }

                if (attempt > 0)
                {
                    throw;
                }
            }
        }
    }

    private static void ValidateUser(string userId) => ArgumentException.ThrowIfNullOrWhiteSpace(userId);
}
