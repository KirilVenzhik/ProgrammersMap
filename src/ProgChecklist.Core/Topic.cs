namespace ProgChecklist.Core;

/// <summary>A single checklist topic.</summary>
/// <param name="Slug">URL-safe identifier, unique within a section.</param>
/// <param name="Title">Display title.</param>
/// <param name="Level">Difficulty level.</param>
public sealed record Topic(string Slug, string Title, Level Level);
