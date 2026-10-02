namespace ProgChecklist.Web.Tests;

/// <summary>Settings that isolate a test from the real content in <c>content/</c>.</summary>
internal static class NoContent
{
    /// <summary>A path that does not exist, which the lesson store treats as "no lessons".</summary>
    public static string LessonsPath() =>
        Path.Combine(Path.GetTempPath(), $"no-lessons-{Guid.NewGuid():N}");

    public static Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> WithoutLessons(
        this Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> factory) =>
        factory.WithWebHostBuilder(b => b.UseSetting("Content:LessonsPath", LessonsPath()));
}
