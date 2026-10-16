namespace ProgChecklist.Data;

/// <summary>A completed topic with its completion time, as exported to the user.</summary>
public sealed record ProgressEntry(string TopicKey, DateTimeOffset CompletedAt);
