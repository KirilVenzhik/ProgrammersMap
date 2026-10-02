namespace ProgChecklist.Core;

/// <summary>A top-level section of the checklist.</summary>
/// <param name="Slug">URL-safe identifier, unique across sections.</param>
/// <param name="Title">Display title.</param>
/// <param name="Order">Sort order, ascending.</param>
/// <param name="Groups">Groups of the section.</param>
public sealed record Section(string Slug, string Title, int Order, IReadOnlyList<Group> Groups)
{
    /// <summary>Total number of topics across all groups.</summary>
    public int TopicCount => Groups.Sum(group => group.Topics.Count);
}
