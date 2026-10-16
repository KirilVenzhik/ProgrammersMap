using System.Text.RegularExpressions;

namespace ProgChecklist.Web.Tests;

/// <summary>Server-rendered markup that progress.js sync (ADR-14) relies on.</summary>
public class SyncMarkupTests : IClassFixture<AppFactory>
{
    private const string GuestFooter = "Прогресс сохраняется в этом браузере.";
    private const string SignedInFooter = "Прогресс сохраняется в аккаунте.";
    private const string ModuleScript = "<script type=\"module\" src=\"/js/progress.js";

    private readonly AppFactory _factory;

    public SyncMarkupTests(AppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_Root_Guest_FooterSaysBrowserAndHasModuleScript()
    {
        // Arrange
        using var client = AuthTestHelpers.NewClient(_factory);

        // Act
        var body = await client.GetStringAsync("/");

        // Assert
        Assert.Contains(GuestFooter, body);
        Assert.DoesNotContain(SignedInFooter, body);
        Assert.Contains(ModuleScript, body);
    }

    [Fact]
    public async Task Get_Root_SignedIn_FooterSaysAccountAndHasModuleScript()
    {
        // Arrange
        using var client = AuthTestHelpers.NewClient(_factory);
        await AuthTestHelpers.SignInNewUserAsync(_factory, client);

        // Act
        var body = await client.GetStringAsync("/");

        // Assert
        Assert.Contains(SignedInFooter, body);
        Assert.DoesNotContain(GuestFooter, body);
        Assert.Contains(ModuleScript, body);
    }

    [Fact]
    public async Task Get_Root_SignedIn_AccountBarLogoutFormHasDataLogout()
    {
        // Arrange
        using var client = AuthTestHelpers.NewClient(_factory);
        await AuthTestHelpers.SignInNewUserAsync(_factory, client);

        // Act
        var body = await client.GetStringAsync("/");

        // Assert
        Assert.Matches(new Regex("<form[^>]*class=\"account-logout\"[^>]*\\bdata-logout\\b"), body);
    }

    [Fact]
    public async Task Get_LogoutPage_SignedIn_FormHasDataLogout()
    {
        // Arrange
        using var client = AuthTestHelpers.NewClient(_factory);
        await AuthTestHelpers.SignInNewUserAsync(_factory, client);

        // Act
        var body = await client.GetStringAsync("/account/logout");

        // Assert
        Assert.Matches(new Regex("<form(?![^>]*account-logout)[^>]*\\bdata-logout\\b"), body);
    }
}
