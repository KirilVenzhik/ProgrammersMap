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
        // Arrange
        var json = One(ValidFields);

        // Act
        var resource = Assert.Single(JsonResourceCatalog.Parse(json, Topics).GetResources("tools", "git"));

        // Assert
        Assert.True(resource.IsFree);
        Assert.False(resource.IsAffiliate);
    }

    [Fact]
    public void Parse_FieldNamesInDifferentCase_StillAccepted()
    {
        // Arrange
        var json = One("\"Title\": \"T\", \"URL\": \"https://a.example\", \"Kind\": \"docs\", \"Lang\": \"en\"");

        // Act
        var catalog = JsonResourceCatalog.Parse(json, Topics);

        // Assert
        Assert.Single(catalog.GetResources("tools", "git"));
    }

    [Fact]
    public void GetResources_MixedCaseSlugs_FindsResources()
    {
        // Arrange
        var catalog = JsonResourceCatalog.Parse(One(ValidFields), Topics);

        // Act
        var list = catalog.GetResources("TOOLS", "Git");

        // Assert
        Assert.Single(list);
    }

    [Theory]
    [InlineData("tools", "unknown")]
    [InlineData("unknown", "git")]
    [InlineData("tools", "branches")]
    [InlineData("", "git")]
    [InlineData("tools", " ")]
    [InlineData(null, null)]
    public void GetResources_NoMatchingResources_ReturnsEmpty(string? section, string? topic)
    {
        // Arrange
        var catalog = JsonResourceCatalog.Parse(One(ValidFields), Topics);

        // Act
        var list = catalog.GetResources(section!, topic!);

        // Assert
        Assert.Empty(list);
    }

    [Fact]
    public void GetResources_ValidTopic_ReturnsReadOnlyList()
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
        // Arrange
        var json = Wrap("{}");

        // Act
        var catalog = JsonResourceCatalog.Parse(json, Topics);

        // Assert
        Assert.Equal(0, catalog.TopicsWithResourcesCount);
    }

    [Fact]
    public void Parse_InvalidJson_ThrowsInvalid()
    {
        // Arrange
        var json = "{ not json";

        // Act
        var ex = Fail(json);

        // Assert
        Assert.Contains("invalid", ex.Message);
    }

    [Fact]
    public void Parse_MissingResourcesObject_ThrowsNamingResources()
    {
        // Arrange
        var json = "{ \"version\": 1 }";

        // Act
        var ex = Fail(json);

        // Assert
        Assert.Contains("resources", ex.Message);
    }

    [Theory]
    [InlineData("2")]
    [InlineData("0")]
    public void Parse_UnsupportedVersion_ThrowsNamingVersion(string version)
    {
        // Arrange
        var json = Wrap("{}", version);

        // Act
        var ex = Fail(json);

        // Assert
        Assert.Contains($"'{version}'", ex.Message);
    }

    [Fact]
    public void Parse_MissingVersion_ThrowsNamingVersion()
    {
        // Arrange
        var json = "{ \"resources\": {} }";

        // Act
        var ex = Fail(json);

        // Assert
        Assert.Contains("version", ex.Message);
    }

    [Theory]
    [InlineData("git")]
    [InlineData("tools/vcs/git")]
    [InlineData("tools/")]
    public void Parse_BadKeyForm_ThrowsNamingKey(string key)
    {
        // Arrange
        var json = Wrap("{ \"" + key + "\": [ { " + ValidFields + " } ] }");

        // Act
        var ex = Fail(json);

        // Assert
        Assert.Contains($"'{key}'", ex.Message);
    }

    [Fact]
    public void Parse_UnknownTopic_ThrowsNamingKey()
    {
        // Arrange
        var json = Wrap("{ \"tools/nope\": [ { " + ValidFields + " } ] }");

        // Act
        var ex = Fail(json);

        // Assert
        Assert.Contains("'tools/nope'", ex.Message);
    }

    [Fact]
    public void Parse_NonCanonicalKeyCase_ThrowsSuggestingCanonical()
    {
        // Arrange
        var json = Wrap("{ \"Tools/git\": [ { " + ValidFields + " } ] }");

        // Act
        var message = Fail(json).Message;

        // Assert
        Assert.Contains("'Tools/git'", message);
        Assert.Contains("tools/git", message);
    }

    [Fact]
    public void Parse_EmptyArray_ThrowsNamingKey()
    {
        // Arrange
        var json = Wrap("{ \"tools/git\": [] }");

        // Act
        var ex = Fail(json);

        // Assert
        Assert.Contains("'tools/git'", ex.Message);
    }

    [Theory]
    [InlineData("\"url\": \"https://a.example\", \"kind\": \"docs\", \"lang\": \"en\"")]
    [InlineData("\"title\": \"  \", \"url\": \"https://a.example\", \"kind\": \"docs\", \"lang\": \"en\"")]
    public void Parse_MissingTitle_ThrowsNamingTitle(string fields)
    {
        // Arrange
        var json = One(fields);

        // Act
        var ex = Fail(json);

        // Assert
        Assert.Contains("title", ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("/relative/path")]
    [InlineData("example.com/page")]
    [InlineData("http://a.example/")]
    [InlineData("ftp://a.example/")]
    public void Parse_BadUrl_ThrowsNamingUrlAndKey(string url)
    {
        // Arrange
        var json = One("\"title\": \"T\", \"url\": \"" + url + "\", \"kind\": \"docs\", \"lang\": \"en\"");

        // Act
        var message = Fail(json).Message;

        // Assert
        Assert.Contains("url", message);
        Assert.Contains("'tools/git'", message);
    }

    [Fact]
    public void Parse_MissingUrl_ThrowsNamingUrl()
    {
        // Arrange
        var json = One("\"title\": \"T\", \"kind\": \"docs\", \"lang\": \"en\"");

        // Act
        var ex = Fail(json);

        // Assert
        Assert.Contains("url", ex.Message);
    }

    [Theory]
    [InlineData("\"kind\": \"movie\", ")]
    [InlineData("")]
    public void Parse_BadKind_ThrowsNamingKind(string kindField)
    {
        // Arrange
        var json = One("\"title\": \"T\", \"url\": \"https://a.example\", " + kindField + "\"lang\": \"en\"");

        // Act
        var ex = Fail(json);

        // Assert
        Assert.Contains("kind", ex.Message);
    }

    [Theory]
    [InlineData("\"lang\": \"de\"")]
    [InlineData("\"lang\": \"\"")]
    public void Parse_BadLang_ThrowsNamingLanguage(string langField)
    {
        // Arrange
        var json = One("\"title\": \"T\", \"url\": \"https://a.example\", \"kind\": \"docs\", " + langField);

        // Act
        var ex = Fail(json);

        // Assert
        Assert.Contains("language", ex.Message);
    }

    [Fact]
    public void Parse_MissingLang_ThrowsNamingLanguage()
    {
        // Arrange
        var json = One("\"title\": \"T\", \"url\": \"https://a.example\", \"kind\": \"docs\"");

        // Act
        var ex = Fail(json);

        // Assert
        Assert.Contains("language", ex.Message);
    }

    [Fact]
    public void Parse_DuplicateUrlInSameTopic_ThrowsDuplicate()
    {
        // Arrange
        var json = Wrap("""
            { "tools/git": [
              { "title": "A", "url": "https://Example.com/x", "kind": "docs", "lang": "en" },
              { "title": "B", "url": "https://example.com/x", "kind": "docs", "lang": "en" }
            ] }
            """);

        // Act
        var message = Fail(json).Message;

        // Assert
        Assert.Contains("Duplicate", message);
        Assert.Contains("'tools/git'", message);
    }

    [Fact]
    public void Parse_UrlsDifferingOnlyByFragment_ThrowsDuplicate()
    {
        // Arrange
        var json = Wrap("""
            { "tools/git": [
              { "title": "A", "url": "https://example.com/x#x", "kind": "docs", "lang": "en" },
              { "title": "B", "url": "https://example.com/x#y", "kind": "docs", "lang": "en" }
            ] }
            """);

        // Act
        var message = Fail(json).Message;

        // Assert
        Assert.Contains("Duplicate", message);
    }

    [Fact]
    public void Parse_SameUrlInDifferentTopics_IsAllowed()
    {
        // Arrange
        var json = Wrap("{ \"tools/git\": [ { " + ValidFields + " } ], \"tools/branches\": [ { " + ValidFields + " } ] }");

        // Act
        var catalog = JsonResourceCatalog.Parse(json, Topics);

        // Assert
        Assert.Equal(2, catalog.TopicsWithResourcesCount);
    }

    [Fact]
    public void Parse_UnknownFieldInResource_ThrowsNamingField()
    {
        // Arrange
        var json = One(ValidFields + ", \"afiliate\": true");

        // Act
        var ex = Fail(json);

        // Assert
        Assert.Contains("afiliate", ex.Message);
    }

    [Fact]
    public void Parse_UnknownRootField_ThrowsNamingField()
    {
        // Arrange
        var json = "{ \"version\": 1, \"resources\": {}, \"extra\": 1 }";

        // Act
        var ex = Fail(json);

        // Assert
        Assert.Contains("extra", ex.Message);
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
    public void LoadFromFile_RealFile_AllResourcesHaveHttpsUrlAndTitle()
    {
        // Arrange
        var topics = JsonTopicCatalog.LoadFromFile(Path.Combine(AppContext.BaseDirectory, "content", "topics.json"));
        var path = Path.Combine(AppContext.BaseDirectory, "content", "resources.json");

        // Act
        var catalog = JsonResourceCatalog.LoadFromFile(path, topics);
        var all = topics.GetSections()
            .SelectMany(s => s.Groups.SelectMany(g => g.Topics.SelectMany(t => catalog.GetResources(s.Slug, t.Slug))))
            .ToList();

        // Assert
        Assert.All(all, r =>
        {
            Assert.Equal(Uri.UriSchemeHttps, r.Url.Scheme);
            Assert.False(string.IsNullOrWhiteSpace(r.Title));
        });
    }
}
