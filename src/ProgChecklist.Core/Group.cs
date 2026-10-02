namespace ProgChecklist.Core;

/// <summary>A group of topics inside a section.</summary>
/// <param name="Slug">URL-safe identifier, unique within a section.</param>
/// <param name="Title">Display title.</param>
/// <param name="Topics">Topics of the group.</param>
public sealed record Group(string Slug, string Title, IReadOnlyList<Topic> Topics);
