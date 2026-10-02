namespace ProgChecklist.Core.Tests;

public class JsonResourceCatalogTests
{
    private const string TopicsJson = """
        {
          "sections": [
            {
              "slug": "tools", "title": "Tools", "order": 1,
              "groups": [
                { "slug": "vcs", "title": "VCS", "topics": [
                  { "slug": "git", "title": "Git", "level": "junior" },
                  { "slug": "branches", "title": "Branches", "level": "junior" }
                ] }
              ]
            }
          ]
        }
        """;

    private static readonly ITopicCatalog Topics = JsonTopicCatalog.Parse(TopicsJson);

    private static string Wrap(string resources, string version = "1") =>
        "{ \"version\": " + version + ", \"resources\": " + resources + " }";

    private static string One(string fields) =>
        Wrap("{ \"tools/git\": [ { " + fields + " } ] }");

    private const string ValidFields =
        "\"title\": \"Pro Git\", \"url\": \"https://git-scm.com/book\", \"kind\": \"book\", \"lang\": \"en\"";

    private static ResourceCatalogException Fail(string json) =>
        Assert.Throws<ResourceCatalogException>(() => JsonResourceCatalog.Parse(json, Topics));

    [Fact]
    public void Parse_Valid_PreservesOrderAndMapsFields()
    {
        // Arrange
        var json = Wrap("""
            {
              "tools/git": [
                { "title": "B", "url": "https://b.example/x", "kind": "video", "lang": "ru", "free": false, "affiliate": true },
                { "title": "A", "url": "https://a.example/x", "kind": "docs", "lang": "en" }
              ],
              "tools/branches": [
                { "title": "C", "url": "https://c.example/", "kind": "practice", "lang": "en" }
              ]
            }
            """);

        // Act
        var catalog = JsonResourceCatalog.Parse(json, Topics);
        var list = catalog.GetResources("tools", "git");

        // Assert
        Assert.Equal(2, catalog.TopicsWithResourcesCount);
        Assert.Equal(["B", "A"], list.Select(r => r.Title));
        Assert.Equal(new Resource("B", new Uri("https://b.example/x"), ResourceKind.Video, ResourceLanguage.Russian, false, true), list[0]);
        Assert.Equal(ResourceKind.Docs, list[1].Kind);
    }

    [Fact]
    public void Parse_OptionalFlagsOmitted_DefaultsToFreeAndNotAffiliate()
    {
        // Arrange & Act
        var resource = Assert.Single(JsonResourceCatalog.Parse(One(ValidFields), Topics).GetResources("tools", "git"));

        // Assert
        Assert.True(resource.IsFree);
        Assert.False(resource.IsAffiliate);
    }

    [Fact]
    public void GetResources_IsCaseInsensitive()
    {
        // Arrange
        var catalog = JsonResourceCatalog.Parse(One(ValidFields), Topics);

        // Act & Assert
        Assert.Single(catalog.GetResources("TOOLS", "Git"));
    }

    [Theory]
    [InlineData("tools", "unknown")]
    [InlineData("unknown", "git")]
    [InlineData("tools", "branches")]
    [InlineData("", "git")]
    [InlineData("tools", " ")]
    [InlineData(null, null)]
    public void GetResources_NoResources_ReturnsEmpty(string? section, string? topic)
    {
        // Arrange
        var catalog = JsonResourceCatalog.Parse(One(ValidFields), Topics);

        // Act
        var list = catalog.GetResources(section!, topic!);

        // Assert
        Assert.Empty(list);
    }

    [Fact]
    public void GetResources_ReturnsReadOnlyList()
    {
        // Arrange
        var catalog = JsonResourceCatalog.Parse(One(ValidFields), Topics);

        // Act
        var list = catalog.GetResources("tools", "git");

        // Assert
        Assert.False(list is IList<Resource> writable && !writable.IsReadOnly);
    }

    [Fact]
    public void Parse_EmptyResourcesObject_HasZeroTopics()
    {
        // Arrange & Act
        var catalog = JsonResourceCatalog.Parse(Wrap("{}"), Topics);

        // Assert
        Assert.Equal(0, catalog.TopicsWithResourcesCount);
    }

    [Fact]
    public void Parse_InvalidJson_Throws()
    {
        Assert.Contains("invalid", Fail("{ not json").Message);
    }

    [Fact]
    public void Parse_MissingResourcesObject_Throws()
    {
        Assert.Contains("resources", Fail("{ \"version\": 1 }").Message);
    }

    [Theory]
    [InlineData("2")]
    [InlineData("0")]
    public void Parse_UnsupportedVersion_Throws(string version)
    {
        Assert.Contains($"'{version}'", Fail(Wrap("{}", version)).Message);
    }

    [Fact]
    public void Parse_MissingVersion_Throws()
    {
        Assert.Contains("version", Fail("{ \"resources\": {} }").Message);
    }

    [Theory]
    [InlineData("git")]
    [InlineData("tools/vcs/git")]
    [InlineData("tools/")]
    public void Parse_BadKeyForm_Throws(string key)
    {
        var json = Wrap("{ \"" + key + "\": [ { " + ValidFields + " } ] }");
        Assert.Contains($"'{key}'", Fail(json).Message);
    }

    [Fact]
    public void Parse_UnknownTopic_Throws()
    {
        var json = Wrap("{ \"tools/nope\": [ { " + ValidFields + " } ] }");
        Assert.Contains("'tools/nope'", Fail(json).Message);
    }

    [Fact]
    public void Parse_NonCanonicalKeyCase_Throws()
    {
        var json = Wrap("{ \"Tools/git\": [ { " + ValidFields + " } ] }");
        var message = Fail(json).Message;
        Assert.Contains("'Tools/git'", message);
        Assert.Contains("tools/git", message);
    }

    [Fact]
    public void Parse_EmptyArray_Throws()
    {
        Assert.Contains("'tools/git'", Fail(Wrap("{ \"tools/git\": [] }")).Message);
    }

    [Theory]
    [InlineData("\"url\": \"https://a.example\", \"kind\": \"docs\", \"lang\": \"en\"")]
    [InlineData("\"title\": \"  \", \"url\": \"https://a.example\", \"kind\": \"docs\", \"lang\": \"en\"")]
    public void Parse_MissingTitle_Throws(string fields)
    {
        Assert.Contains("title", Fail(One(fields)).Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("/relative/path")]
    [InlineData("example.com/page")]
    [InlineData("http://a.example/")]
    [InlineData("ftp://a.example/")]
    public void Parse_BadUrl_Throws(string url)
    {
        var json = One("\"title\": \"T\", \"url\": \"" + url + "\", \"kind\": \"docs\", \"lang\": \"en\"");
        var message = Fail(json).Message;
        Assert.Contains("url", message);
        Assert.Contains("'tools/git'", message);
    }

    [Fact]
    public void Parse_MissingUrl_Throws()
    {
        Assert.Contains("url", Fail(One("\"title\": \"T\", \"kind\": \"docs\", \"lang\": \"en\"")).Message);
    }

    [Theory]
    [InlineData("\"kind\": \"movie\", ")]
    [InlineData("")]
    public void Parse_BadKind_Throws(string kindField)
    {
        var json = One("\"title\": \"T\", \"url\": \"https://a.example\", " + kindField + "\"lang\": \"en\"");
        Assert.Contains("kind", Fail(json).Message);
    }

    [Theory]
    [InlineData("\"lang\": \"de\"")]
    [InlineData("\"lang\": \"\"")]
    public void Parse_BadLang_Throws(string langField)
    {
        var json = One("\"title\": \"T\", \"url\": \"https://a.example\", \"kind\": \"docs\", " + langField);
        Assert.Contains("language", Fail(json).Message);
    }

    [Fact]
    public void Parse_MissingLang_Throws()
    {
        var json = One("\"title\": \"T\", \"url\": \"https://a.example\", \"kind\": \"docs\"");
        Assert.Contains("language", Fail(json).Message);
    }

    [Fact]
    public void Parse_DuplicateUrlInSameTopic_Throws()
    {
        var json = Wrap("""
            { "tools/git": [
              { "title": "A", "url": "https://Example.com/x", "kind": "docs", "lang": "en" },
              { "title": "B", "url": "https://example.com/x", "kind": "docs", "lang": "en" }
            ] }
            """);
        var message = Fail(json).Message;
        Assert.Contains("Duplicate", message);
        Assert.Contains("'tools/git'", message);
    }

    [Fact]
    public void Parse_SameUrlInDifferentTopics_IsAllowed()
    {
        var json = Wrap("{ \"tools/git\": [ { " + ValidFields + " } ], \"tools/branches\": [ { " + ValidFields + " } ] }");
        Assert.Equal(2, JsonResourceCatalog.Parse(json, Topics).TopicsWithResourcesCount);
    }

    [Fact]
    public void LoadFromFile_Missing_ThrowsWithPath()
    {
        // Arrange
        var path = Path.Combine(AppContext.BaseDirectory, "missing", "resources.json");

        // Act
        var ex = Assert.Throws<ResourceCatalogException>(() => JsonResourceCatalog.LoadFromFile(path, Topics));

        // Assert
        Assert.Contains(path, ex.Message);
    }

    [Fact]
    public void LoadFromFile_RealFile_LoadsAgainstRealTopics()
    {
        // Arrange
        var topics = JsonTopicCatalog.LoadFromFile(Path.Combine(AppContext.BaseDirectory, "content", "topics.json"));
        var path = Path.Combine(AppContext.BaseDirectory, "content", "resources.json");

        // Act
        var catalog = JsonResourceCatalog.LoadFromFile(path, topics);

        // Assert
        Assert.True(catalog.TopicsWithResourcesCount >= 0);
    }
}
