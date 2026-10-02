using System.Text.Json;

namespace ProgChecklist.Core.Tests;

public class BreadcrumbJsonLdTests
{
    private const string Json = """
        { "sections": [
          { "slug": "sec", "title": "Section </script>", "order": 1, "groups": [
            { "slug": "g", "title": "G", "topics": [ { "slug": "top", "title": "Topic <b>&</b>", "level": "junior" } ] } ] } ] }
        """;

    [Fact]
    public void Build_Topic_ProducesBreadcrumbListWithThreeItems()
    {
        // Arrange
        var catalog = JsonTopicCatalog.Parse(Json);
        var context = catalog.FindTopicContext("sec", "top")!;

        // Act
        var output = BreadcrumbJsonLd.Build(context, path => "https://x.test" + path);
        using var document = JsonDocument.Parse(output);
        var root = document.RootElement;
        var items = root.GetProperty("itemListElement").EnumerateArray().ToList();

        // Assert
        Assert.Equal("https://schema.org", root.GetProperty("@context").GetString());
        Assert.Equal("BreadcrumbList", root.GetProperty("@type").GetString());
        Assert.Equal(new[] { 1, 2, 3 }, items.Select(item => item.GetProperty("position").GetInt32()));
        Assert.Equal(new[] { "Карта программиста", "Section </script>", "Topic <b>&</b>" }, items.Select(item => item.GetProperty("name").GetString()));
        Assert.Equal(
            new[] { "https://x.test/", "https://x.test/sec", "https://x.test/sec/top" },
            items.Select(item => item.GetProperty("item").GetString()));
    }

    [Fact]
    public void Build_TitleWithScriptEndTag_IsEscaped()
    {
        // Arrange
        var context = JsonTopicCatalog.Parse(Json).FindTopicContext("sec", "top")!;

        // Act
        var output = BreadcrumbJsonLd.Build(context, path => path);

        // Assert
        Assert.DoesNotContain("</script>", output, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<", output);
    }
}
