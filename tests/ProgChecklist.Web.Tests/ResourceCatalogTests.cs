using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ProgChecklist.Core;

namespace ProgChecklist.Web.Tests;

public class ResourceCatalogTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ResourceCatalogTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public void App_StartsWithRealResourcesFile_AndCatalogIsSingleton()
    {
        // Arrange
        var services = _factory.Services;

        // Act
        var first = services.GetRequiredService<IResourceCatalog>();
        var second = services.GetRequiredService<IResourceCatalog>();

        // Assert
        Assert.Same(first, second);
    }

    [Fact]
    public void App_WithBadResourcesFile_FailsAtStartup()
    {
        // Arrange
        var path = Path.Combine(Path.GetTempPath(), $"bad-resources-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, "{ \"version\": 1, \"resources\": { \"nope/nope\": [] } }");
        try
        {
            var factory = _factory.WithWebHostBuilder(builder => builder.UseSetting("Content:ResourcesPath", path));

            // Act & Assert
            Assert.Throws<ResourceCatalogException>(() => factory.CreateClient());
        }
        finally
        {
            File.Delete(path);
        }
    }
}
