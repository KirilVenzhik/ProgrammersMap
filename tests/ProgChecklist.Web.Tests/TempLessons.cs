namespace ProgChecklist.Web.Tests;

/// <summary>A temporary lessons directory containing one Markdown file; deleted on dispose.</summary>
internal sealed class TempLessons : IDisposable
{
    public TempLessons(string relativePath, string content)
        : this((relativePath, content))
    {
    }

    public TempLessons(params (string RelativePath, string Content)[] files)
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"web-lessons-{Guid.NewGuid():N}");
        foreach (var (relativePath, content) in files)
        {
            var full = System.IO.Path.Combine(Path, relativePath.Replace('/', System.IO.Path.DirectorySeparatorChar));
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(full)!);
            File.WriteAllText(full, content);
        }
    }

    public string Path { get; }

    public void Dispose() => Directory.Delete(Path, recursive: true);
}
