namespace ProgChecklist.Core;

/// <summary>Read-only access to topic lessons.</summary>
public interface ILessonStore
{
    /// <summary>Number of loaded lessons.</summary>
    int Count { get; }

    /// <summary>Finds a lesson by section and topic slug (case-insensitive); null if there is none.</summary>
    Lesson? FindLesson(string sectionSlug, string topicSlug);

    /// <summary>True if the topic has a lesson.</summary>
    bool HasLesson(string sectionSlug, string topicSlug);
}
