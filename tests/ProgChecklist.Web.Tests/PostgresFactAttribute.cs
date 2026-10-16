namespace ProgChecklist.Web.Tests;

/// <summary>
/// A fact that needs a real PostgreSQL server (ADR-12). Skipped unless the environment variable
/// <see cref="EnvironmentVariable"/> holds a connection string.
/// </summary>
public sealed class PostgresFactAttribute : FactAttribute
{
    public const string EnvironmentVariable = "PROGCHECKLIST_TEST_POSTGRES";

    public PostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(EnvironmentVariable)))
        {
            Skip = $"Set {EnvironmentVariable} to a PostgreSQL connection string to run";
        }
    }
}
