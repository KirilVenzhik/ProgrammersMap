namespace ProgChecklist.Core;

/// <summary>Thrown when the resource catalog cannot be loaded or is invalid.</summary>
public sealed class ResourceCatalogException : Exception
{
    /// <summary>Creates the exception with a message.</summary>
    public ResourceCatalogException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception with a message and an inner exception.</summary>
    public ResourceCatalogException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
