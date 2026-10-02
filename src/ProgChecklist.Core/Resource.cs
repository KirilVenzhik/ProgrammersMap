namespace ProgChecklist.Core;

/// <summary>An external learning resource attached to a topic.</summary>
/// <param name="Title">Display title.</param>
/// <param name="Url">Absolute https URL.</param>
/// <param name="Kind">Resource type.</param>
/// <param name="Language">Resource language.</param>
/// <param name="IsFree">Whether the resource is free.</param>
/// <param name="IsAffiliate">Whether the link is an affiliate (sponsored) link.</param>
public sealed record Resource(
    string Title,
    Uri Url,
    ResourceKind Kind,
    ResourceLanguage Language,
    bool IsFree,
    bool IsAffiliate);
