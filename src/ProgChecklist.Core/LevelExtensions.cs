namespace ProgChecklist.Core;

/// <summary>Presentation helpers for <see cref="Level"/>.</summary>
public static class LevelExtensions
{
    /// <summary>Human-readable name: Junior, Middle, Senior.</summary>
    public static string ToDisplayName(this Level level) => level switch
    {
        Level.Junior => "Junior",
        Level.Middle => "Middle",
        Level.Senior => "Senior",
        _ => throw new ArgumentOutOfRangeException(nameof(level), level, null),
    };

    /// <summary>Lowercase code used in data attributes: junior, middle, senior.</summary>
    public static string ToCode(this Level level) => level switch
    {
        Level.Junior => "junior",
        Level.Middle => "middle",
        Level.Senior => "senior",
        _ => throw new ArgumentOutOfRangeException(nameof(level), level, null),
    };
}
