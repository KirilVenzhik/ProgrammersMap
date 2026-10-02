namespace ProgChecklist.Core;

/// <summary>Presentation helpers for <see cref="ResourceLanguage"/>.</summary>
public static class ResourceLanguageExtensions
{
    /// <summary>Russian label of the resource language.</summary>
    public static string ToDisplayName(this ResourceLanguage language) => language switch
    {
        ResourceLanguage.Russian => "на русском",
        ResourceLanguage.English => "на английском",
        _ => throw new ArgumentOutOfRangeException(nameof(language), language, null),
    };
}
