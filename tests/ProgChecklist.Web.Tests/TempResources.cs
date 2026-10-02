namespace ProgChecklist.Web.Tests;

/// <summary>A temporary resources.json file; deleted on dispose.</summary>
internal sealed class TempResources : IDisposable
{
    private readonly string _directory;

    public TempResources(string json)
    {
        _directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"web-resources-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_directory);
        Path = System.IO.Path.Combine(_directory, "resources.json");
        File.WriteAllText(Path, json);
    }

    public string Path { get; }

    public void Dispose() => Directory.Delete(_directory, recursive: true);
}
