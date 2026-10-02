namespace ProgChecklist.Core;

/// <summary>Thrown when the topic catalog cannot be loaded or is invalid.</summary>
public sealed class TopicCatalogException : Exception
{
    /// <summary>Creates the exception with a message.</summary>
    public TopicCatalogException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception with a message and an inner exception.</summary>
    public TopicCatalogException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
