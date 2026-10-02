using Microsoft.AspNetCore.Mvc.Testing;

namespace ProgChecklist.Web.Tests;

public class SmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SmokeTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_Root_ReturnsSuccess()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        using var response = await client.GetAsync("/");

        // Assert
        Assert.True(response.IsSuccessStatusCode);
    }
}
