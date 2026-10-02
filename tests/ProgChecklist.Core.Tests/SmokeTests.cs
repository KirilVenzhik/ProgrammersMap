using ProgChecklist.Core;

namespace ProgChecklist.Core.Tests;

public class SmokeTests
{
    [Fact]
    public void CoreAssembly_WhenLoaded_HasExpectedName()
    {
        // Arrange
        var assembly = typeof(AssemblyMarker).Assembly;

        // Act
        var name = assembly.GetName().Name;

        // Assert
        Assert.Equal("ProgChecklist.Core", name);
    }
}
