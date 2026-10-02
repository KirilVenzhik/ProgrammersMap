namespace ProgChecklist.Core;

/// <summary>Type of a learning resource (JSON: docs, article, video, course, book, practice).</summary>
public enum ResourceKind
{
    /// <summary>Official documentation.</summary>
    Docs,

    /// <summary>Article or blog post.</summary>
    Article,

    /// <summary>Video or video series.</summary>
    Video,

    /// <summary>Online course.</summary>
    Course,

    /// <summary>Book.</summary>
    Book,

    /// <summary>Practice platform or exercises.</summary>
    Practice,
}
