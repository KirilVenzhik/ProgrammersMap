namespace ProgChecklist.Data;

/// <summary>Server-side storage of completed topics per user (ADR-14).</summary>
public interface IProgressService
{
    /// <summary>The user's completed topic keys, sorted ordinal.</summary>
    Task<IReadOnlyList<string>> GetKeysAsync(string userId, CancellationToken ct = default);

    /// <summary>The user's completed topics with completion times, sorted by key ordinal.</summary>
    Task<IReadOnlyList<ProgressEntry>> GetEntriesAsync(string userId, CancellationToken ct = default);

    /// <summary>Marks a topic done (idempotent). False when the key is unknown; nothing is written then.</summary>
    Task<bool> MarkAsync(string userId, string topicKey, CancellationToken ct = default);

    /// <summary>Unmarks a topic (idempotent). False when the key is unknown.</summary>
    Task<bool> UnmarkAsync(string userId, string topicKey, CancellationToken ct = default);

    /// <summary>Deletes all progress of the user.</summary>
    Task ClearAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Merges keys into the user's progress (unknown/duplicate keys dropped, existing rows untouched).
    /// Throws <see cref="ArgumentException"/> when more than <see cref="ProgressService.MaxMergeKeys"/> keys are given.
    /// Returns the full set, sorted ordinal.
    /// </summary>
    Task<IReadOnlyList<string>> MergeAsync(string userId, IReadOnlyCollection<string?> keys, CancellationToken ct = default);
}
