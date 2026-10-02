namespace ProgChecklist.Core;

/// <summary>Picks the correct Russian noun form for a number.</summary>
public static class RussianPlural
{
    /// <summary>Returns <paramref name="one"/> (1, 21), <paramref name="few"/> (2-4, 22-24) or <paramref name="many"/> (otherwise).</summary>
    public static string Choose(int n, string one, string few, string many)
    {
        var abs = Math.Abs((long)n);
        var lastTwo = abs % 100;
        if (lastTwo is >= 11 and <= 14)
        {
            return many;
        }

        return (abs % 10) switch
        {
            1 => one,
            >= 2 and <= 4 => few,
            _ => many,
        };
    }
}
