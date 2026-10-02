namespace ProgChecklist.Core;

/// <summary>Thrown when lessons cannot be loaded or are invalid.</summary>
public sealed class LessonStoreException : Exception
{
    /// <summary>Creates the exception with a message.</summary>
    public LessonStoreException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception with a message and an inner exception.</summary>
    public LessonStoreException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
