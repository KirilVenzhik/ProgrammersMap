namespace ProgChecklist.Data;

/// <summary>A topic a user has marked as done. <see cref="TopicKey"/> is "{section}/{topic}".</summary>
public class UserTopicProgress
{
    public const int TopicKeyMaxLength = 100;

    public string UserId { get; set; } = string.Empty;

    public string TopicKey { get; set; } = string.Empty;

    public DateTimeOffset CompletedAt { get; set; }

    public AppUser? User { get; set; }
}
