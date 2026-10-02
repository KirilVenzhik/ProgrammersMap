using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ProgChecklist.Core;

namespace ProgChecklist.Web.Tests;

public class LessonStoreTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public LessonStoreTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public void App_Starts_AndStoreIsSingleton()
    {
        // Arrange
        var services = _factory.Services;

        // Act
        var first = services.GetRequiredService<ILessonStore>();
        var second = services.GetRequiredService<ILessonStore>();

        // Assert
        Assert.Same(first, second);
    }

    [Fact]
    public void App_WithBadLesson_FailsAtStartup()
    {
        // Arrange
        using var dir = new TempLessons("nope/nope.md", "## T\n\ntext");
        var factory = _factory.WithWebHostBuilder(builder => builder.UseSetting("Content:LessonsPath", dir.Path));

        // Act & Assert
        Assert.Throws<LessonStoreException>(() => factory.CreateClient());
    }

    [Fact]
    public void App_WithValidLesson_LoadsIt()
    {
        // Arrange
        using var dir = new TempLessons("git/init-add-commit.md", "## Intro\n\nHello.");
        var factory = _factory.WithWebHostBuilder(builder => builder.UseSetting("Content:LessonsPath", dir.Path));

        // Act
        var store = factory.Services.GetRequiredService<ILessonStore>();

        // Assert
        Assert.Equal(1, store.Count);
        Assert.True(store.HasLesson("git", "init-add-commit"));
    }

    private sealed class TempLessons : IDisposable
    {
        public TempLessons(string relativePath, string content)
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"web-lessons-{Guid.NewGuid():N}");
            var full = System.IO.Path.Combine(Path, relativePath.Replace('/', System.IO.Path.DirectorySeparatorChar));
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(full)!);
            File.WriteAllText(full, content);
        }

        public string Path { get; }

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
