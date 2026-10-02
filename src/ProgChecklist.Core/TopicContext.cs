namespace ProgChecklist.Core;

/// <summary>A topic together with its section, group and neighbours in global catalog order.</summary>
/// <param name="Section">Section that owns the topic.</param>
/// <param name="Group">Group that owns the topic.</param>
/// <param name="Topic">The topic itself.</param>
/// <param name="Previous">Previous topic in global order; null for the first topic.</param>
/// <param name="Next">Next topic in global order; null for the last topic.</param>
public sealed record TopicContext(Section Section, Group Group, Topic Topic, TopicLink? Previous, TopicLink? Next);
