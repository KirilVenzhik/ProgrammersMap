namespace ProgChecklist.Core;

/// <summary>A reference to a topic with its section (enough to build a URL).</summary>
/// <param name="Section">Section that owns the topic.</param>
/// <param name="Topic">The topic.</param>
public sealed record TopicLink(Section Section, Topic Topic);
