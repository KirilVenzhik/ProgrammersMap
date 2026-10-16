using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ProgChecklist.Data;

namespace ProgChecklist.Web.Tests;

public class LoginTests : IClassFixture<AppFactory>
{
    private const string WrongMessage = "Неверная почта или пароль.";

    private readonly AppFactory _factory;

    public LoginTests(AppFactory factory)
    {
        _factory = factory;
    }

    private HttpClient NewClient() => AuthTestHelpers.NewClient(_factory);

    [Fact]
    public async Task Get_Login_ReturnsFormWithTokenAndNoIndex()
    {
        // Arrange
        using var client = NewClient();

        // Act
        var response = await client.GetAsync("/account/login");
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<form", body);
        Assert.Contains("__RequestVerificationToken", body);
        Assert.Contains("<meta name=\"robots\" content=\"noindex\">", body);
        Assert.Contains("autocomplete=\"current-password\"", body);
        Assert.Contains("Запомнить меня", body);
        Assert.Contains("<title>Вход — Карта программиста</title>", body);
    }

    [Fact]
    public async Task Get_Login_WhenSignedIn_RedirectsHome()
    {
        // Arrange
        using var client = NewClient();
        var email = await AuthTestHelpers.CreateUserAsync(_factory);
        await AuthTestHelpers.LoginAsync(client, email, AuthTestHelpers.GoodPassword);

        // Act
        var response = await client.GetAsync("/account/login");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Get_Login_WithReturnUrl_LinksToRegisterWithEncodedReturnUrl()
    {
        // Arrange
        using var client = NewClient();

        // Act
        var body = await client.GetStringAsync("/account/login?returnUrl=%2Fgit%2Finit-add-commit");

        // Assert
        Assert.Contains("href=\"/account/register?returnUrl=%2Fgit%2Finit-add-commit\"", body);
    }

    [Fact]
    public async Task Get_Register_WithReturnUrl_LinksToLoginWithEncodedReturnUrl()
    {
        // Arrange
        using var client = NewClient();

        // Act
        var body = await client.GetStringAsync("/account/register?returnUrl=%2Fgit");

        // Assert
        Assert.Contains("href=\"/account/login?returnUrl=%2Fgit\"", body);
    }

    [Theory]
    [InlineData("/git", "/git")]
    [InlineData("https://evil.example/", "/")]
    [InlineData("//evil.example", "/")]
    [InlineData(@"/\evil.example", "/")]
    public async Task Post_CorrectPassword_RedirectsOnlyToLocalReturnUrlAndSetsCookie(string returnUrl, string expected)
    {
        // Arrange
        using var client = NewClient();
        var email = await AuthTestHelpers.CreateUserAsync(_factory);

        // Act
        var response = await AuthTestHelpers.LoginAsync(client, email, AuthTestHelpers.GoodPassword, returnUrl: returnUrl);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(expected, response.Headers.Location?.OriginalString);
        Assert.NotNull(AuthTestHelpers.AuthCookie(response));
    }

    [Fact]
    public async Task Post_RememberMeOn_CookieIsPersistentWithSecurityFlags()
    {
        // Arrange
        using var client = NewClient();
        var email = await AuthTestHelpers.CreateUserAsync(_factory);

        // Act
        var response = await AuthTestHelpers.LoginAsync(client, email, AuthTestHelpers.GoodPassword, remember: true);

        // Assert
        var cookie = AuthTestHelpers.AuthCookie(response);
        Assert.NotNull(cookie);
        Assert.Contains("expires=", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Post_RememberMeOff_CookieIsSessionCookie()
    {
        // Arrange
        using var client = NewClient();
        var email = await AuthTestHelpers.CreateUserAsync(_factory);

        // Act
        var response = await AuthTestHelpers.LoginAsync(client, email, AuthTestHelpers.GoodPassword, remember: false);

        // Assert
        var cookie = AuthTestHelpers.AuthCookie(response);
        Assert.NotNull(cookie);
        Assert.DoesNotContain("expires=", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Post_WrongPasswordAndUnknownEmail_ShowSameGenericMessage()
    {
        // Arrange
        using var client = NewClient();
        var email = await AuthTestHelpers.CreateUserAsync(_factory);
        var unknown = AuthTestHelpers.NewEmail();

        // Act
        var wrong = await AuthTestHelpers.LoginAsync(client, email, "wrong password");
        var wrongBody = await wrong.Content.ReadAsStringAsync();
        var missing = await AuthTestHelpers.LoginAsync(client, unknown, "wrong password");
        var missingBody = await missing.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, wrong.StatusCode);
        Assert.Equal(HttpStatusCode.OK, missing.StatusCode);
        Assert.Contains(WrongMessage, wrongBody);
        Assert.Contains(WrongMessage, missingBody);
        Assert.Null(AuthTestHelpers.AuthCookie(wrong));
        Assert.Equal(Normalize(wrongBody, email), Normalize(missingBody, unknown));
    }

    [Fact]
    public async Task Post_FiveFailuresThenCorrectPassword_ShowsLockoutAndNoCookie()
    {
        // Arrange
        using var client = NewClient();
        var email = await AuthTestHelpers.CreateUserAsync(_factory);
        for (var i = 0; i < 5; i++)
        {
            await AuthTestHelpers.LoginAsync(client, email, "wrong password");
        }

        // Act
        var response = await AuthTestHelpers.LoginAsync(client, email, AuthTestHelpers.GoodPassword);
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Слишком много неудачных попыток входа", body);
        Assert.Null(AuthTestHelpers.AuthCookie(response));
    }

    [Fact]
    public async Task Post_InvalidEmailFormat_ShowsErrorAndMarksFieldInvalid()
    {
        // Arrange
        using var client = NewClient();

        // Act
        var response = await AuthTestHelpers.LoginAsync(client, "not-an-email", "whatever");
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Некорректный адрес электронной почты", body);
        Assert.Matches("<input(?=[^>]*id=\"Input_Email\")(?=[^>]*aria-invalid=\"true\")[^>]*>", body);
        Assert.Contains("aria-describedby=\"login-email-error\"", body);
        Assert.DoesNotContain("aria-describedby=\"login-password-error\"", body);
    }

    [Fact]
    public async Task Get_Logout_WhileSignedIn_DoesNotSignOut()
    {
        // Arrange
        using var client = NewClient();
        var email = await AuthTestHelpers.CreateUserAsync(_factory);
        await AuthTestHelpers.LoginAsync(client, email, AuthTestHelpers.GoodPassword);

        // Act
        var logoutPage = await client.GetAsync("/account/logout");
        var logoutBody = await logoutPage.Content.ReadAsStringAsync();
        var after = await client.GetStringAsync("/git");

        // Assert
        Assert.Equal(HttpStatusCode.OK, logoutPage.StatusCode);
        Assert.Contains("Выйти из аккаунта?", logoutBody);
        Assert.Contains("<meta name=\"robots\" content=\"noindex\">", logoutBody);
        Assert.Contains(email, after);
    }

    [Fact]
    public async Task Post_Logout_WithToken_SignsOutAndRedirectsHome()
    {
        // Arrange
        using var client = NewClient();
        var email = await AuthTestHelpers.CreateUserAsync(_factory);
        await AuthTestHelpers.LoginAsync(client, email, AuthTestHelpers.GoodPassword);
        var token = await AuthTestHelpers.GetTokenAsync(client, "/account/logout");

        // Act
        var response = await client.PostAsync("/account/logout",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token }));
        var after = await client.GetStringAsync("/git");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.OriginalString);
        Assert.DoesNotContain(email, after);
        Assert.Contains("Войти", after);
    }

    [Fact]
    public async Task Post_Logout_WithoutToken_Returns400AndStaysSignedIn()
    {
        // Arrange
        using var client = NewClient();
        var email = await AuthTestHelpers.CreateUserAsync(_factory);
        await AuthTestHelpers.LoginAsync(client, email, AuthTestHelpers.GoodPassword);

        // Act
        var response = await client.PostAsync("/account/logout", new FormUrlEncodedContent(new Dictionary<string, string>()));
        var after = await client.GetStringAsync("/git");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(email, after);
    }

    [Fact]
    public async Task AccountBar_Guest_ShowsLoginAndRegisterWithReturnUrl()
    {
        // Arrange
        using var client = NewClient();

        // Act
        var body = await client.GetStringAsync("/git/init-add-commit");

        // Assert
        Assert.Contains("<nav class=\"account-nav\" aria-label=\"Аккаунт\">", body);
        Assert.Contains("href=\"/account/login?returnUrl=%2Fgit%2Finit-add-commit\"", body);
        Assert.Contains("href=\"/account/register?returnUrl=%2Fgit%2Finit-add-commit\"", body);
        Assert.DoesNotContain("action=\"/account/logout\"", body);
    }

    [Fact]
    public async Task AccountBar_GuestOnAccountPage_UsesPlainLinks()
    {
        // Arrange
        using var client = NewClient();

        // Act
        var body = await client.GetStringAsync("/account/login?returnUrl=%2Fgit");

        // Assert
        Assert.Contains("href=\"/account/login\"", body);
        Assert.Contains("href=\"/account/register\"", body);
    }

    [Fact]
    public async Task AccountBar_SignedIn_ShowsEmailAndLogoutWithoutLogin()
    {
        // Arrange
        using var client = NewClient();
        var email = await AuthTestHelpers.CreateUserAsync(_factory);
        await AuthTestHelpers.LoginAsync(client, email, AuthTestHelpers.GoodPassword);

        // Act
        var body = await client.GetStringAsync("/git/init-add-commit");

        // Assert
        Assert.Contains($"title=\"{email}\"", body);
        Assert.Contains("action=\"/account/logout\"", body);
        Assert.Contains(">Выйти</button>", body);
        Assert.DoesNotContain("href=\"/account/login", body);
    }

    [Fact]
    public async Task Get_Register_HintIsReferencedByAriaDescribedBy()
    {
        // Arrange
        using var client = NewClient();

        // Act
        var body = await client.GetStringAsync("/account/register");

        // Assert
        Assert.Contains("id=\"reg-password-hint\"", body);
        Assert.Matches("<input(?=[^>]*id=\"Input_Password\")(?=[^>]*aria-describedby=\"reg-password-hint\")[^>]*>", body);
        Assert.DoesNotContain("aria-invalid", body);
    }

    [Fact]
    public async Task Post_Register_Failed_MarksInvalidFieldAndReferencesError()
    {
        // Arrange
        using var client = NewClient();
        var fields = new Dictionary<string, string>
        {
            ["Input.Email"] = AuthTestHelpers.NewEmail(),
            ["Input.Password"] = "short",
            ["Input.ConfirmPassword"] = "short",
            ["__RequestVerificationToken"] = await AuthTestHelpers.GetTokenAsync(client, "/account/register"),
        };

        // Act
        var response = await client.PostAsync("/account/register", new FormUrlEncodedContent(fields));
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Matches("<input(?=[^>]*id=\"Input_Password\")(?=[^>]*aria-invalid=\"true\")[^>]*>", body);
        Assert.Contains("aria-describedby=\"reg-password-hint reg-password-error\"", body);
        Assert.DoesNotMatch("<input(?=[^>]*id=\"Input_Email\")(?=[^>]*aria-invalid)[^>]*>", body);
    }

    private static string Normalize(string body, string email) =>
        Regex.Replace(body, "(name=\"__RequestVerificationToken\"[^>]*value=\")[^\"]+", "$1T")
            .Replace(System.Net.WebUtility.HtmlEncode(email), "EMAIL")
            .Replace(email, "EMAIL");
}
