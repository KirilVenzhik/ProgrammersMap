using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ProgChecklist.Core;
using ProgChecklist.Data;

namespace ProgChecklist.Web.Tests;

public class RegisterTests : IClassFixture<AppFactory>
{
    private const string GoodPassword = "correct horse";

    private readonly AppFactory _factory;

    public RegisterTests(AppFactory factory)
    {
        _factory = factory;
    }

    private static string NewEmail() => $"u{Guid.NewGuid():N}@example.test";

    private HttpClient NewClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private static async Task<string> GetTokenAsync(HttpClient client)
    {
        var html = await client.GetStringAsync("/account/register");
        var match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(match.Success, "antiforgery token not found");
        return match.Groups[1].Value;
    }

    private static async Task<HttpResponseMessage> PostAsync(
        HttpClient client, string email, string password, string confirm, string? returnUrl = null, bool withToken = true)
    {
        var fields = new Dictionary<string, string>
        {
            ["Input.Email"] = email,
            ["Input.Password"] = password,
            ["Input.ConfirmPassword"] = confirm,
        };
        if (returnUrl is not null)
        {
            fields["ReturnUrl"] = returnUrl;
        }

        if (withToken)
        {
            fields["__RequestVerificationToken"] = await GetTokenAsync(client);
        }

        return await client.PostAsync("/account/register", new FormUrlEncodedContent(fields));
    }

    private async Task<int> CountUsersAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Users.CountAsync(u => u.NormalizedEmail == email.ToUpperInvariant());
    }

    [Fact]
    public async Task Get_Register_ReturnsFormWithTokenAndNoIndex()
    {
        // Arrange
        using var client = NewClient();

        // Act
        var response = await client.GetAsync("/account/register");
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<form", body);
        Assert.Contains("__RequestVerificationToken", body);
        Assert.Contains("<meta name=\"robots\" content=\"noindex\">", body);
        Assert.Contains("autocomplete=\"new-password\"", body);
        Assert.Contains("autocomplete=\"email\"", body);
    }

    [Fact]
    public async Task Post_ValidData_RedirectsHomeSetsAuthCookieAndCreatesUser()
    {
        // Arrange
        using var client = NewClient();
        var email = NewEmail();

        // Act
        var response = await PostAsync(client, email, GoodPassword, GoodPassword);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.OriginalString);
        Assert.Contains(response.Headers.GetValues("Set-Cookie"), c => c.StartsWith(".ProgChecklist.Auth="));
        using var scope = _factory.Services.CreateScope();
        var user = await scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>().FindByEmailAsync(email);
        Assert.NotNull(user);
        Assert.Equal(email, user.UserName);
    }

    [Theory]
    [InlineData("/git", "/git")]
    [InlineData("https://evil.example/", "/")]
    [InlineData("//evil.example", "/")]
    public async Task Post_ReturnUrl_OnlyLocalIsHonored(string returnUrl, string expected)
    {
        // Arrange
        using var client = NewClient();

        // Act
        var response = await PostAsync(client, NewEmail(), GoodPassword, GoodPassword, returnUrl);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(expected, response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Post_DuplicateEmailDifferentCase_ShowsRussianErrorOnceAndKeepsOneUser()
    {
        // Arrange
        var email = NewEmail();
        using (var first = NewClient())
        {
            Assert.Equal(HttpStatusCode.Redirect, (await PostAsync(first, email, GoodPassword, GoodPassword)).StatusCode);
        }

        using var client = NewClient();

        // Act
        var response = await PostAsync(client, email.ToUpperInvariant(), GoodPassword, GoodPassword);
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, Regex.Count(body, "Аккаунт с такой почтой уже существует"));
        Assert.Equal(1, await CountUsersAsync(email));
    }

    [Fact]
    public async Task Post_ShortPassword_ReturnsErrorAndDoesNotCreateUser()
    {
        // Arrange
        using var client = NewClient();
        var email = NewEmail();

        // Act
        var response = await PostAsync(client, email, "1234567", "1234567");
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("не менее 8 символов", body);
        Assert.Equal(0, await CountUsersAsync(email));
    }

    [Fact]
    public async Task Post_PasswordMismatch_ReturnsErrorAndDoesNotCreateUser()
    {
        // Arrange
        using var client = NewClient();
        var email = NewEmail();

        // Act
        var response = await PostAsync(client, email, GoodPassword, GoodPassword + "x");
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Пароли не совпадают", body);
        Assert.Equal(0, await CountUsersAsync(email));
    }

    [Fact]
    public async Task Post_InvalidEmail_ReturnsErrorAndDoesNotCreateUser()
    {
        // Arrange
        using var client = NewClient();

        // Act
        var response = await PostAsync(client, "not-an-email", GoodPassword, GoodPassword);
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Некорректный адрес электронной почты", body);
        Assert.Equal(0, await CountUsersAsync("not-an-email"));
    }

    [Fact]
    public async Task Post_WithoutAntiforgeryToken_Returns400()
    {
        // Arrange
        using var client = NewClient();
        var email = NewEmail();

        // Act
        var response = await PostAsync(client, email, GoodPassword, GoodPassword, withToken: false);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await CountUsersAsync(email));
    }

    [Fact]
    public async Task Get_Register_WhenSignedIn_RedirectsHome()
    {
        // Arrange
        using var client = NewClient();
        await PostAsync(client, NewEmail(), GoodPassword, GoodPassword);

        // Act
        var response = await client.GetAsync("/account/register");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Get_Sitemap_DoesNotContainAccountPages()
    {
        // Arrange
        using var client = NewClient();

        // Act
        var body = await client.GetStringAsync("/sitemap.xml");

        // Assert
        Assert.DoesNotContain("/account", body);
    }

    [Fact]
    public void Catalog_SectionSlugs_DoNotCollideWithReservedRoutes()
    {
        // Arrange
        var catalog = JsonTopicCatalog.LoadFromFile(Path.Combine(AppContext.BaseDirectory, "content", "topics.json"));

        // Act
        var slugs = catalog.GetSections().Select(s => s.Slug).ToList();

        // Assert
        Assert.DoesNotContain("account", slugs);
        Assert.DoesNotContain("api", slugs);
    }

    [Theory]
    [InlineData("/api/progress", StatusCodes.Status401Unauthorized, false)]
    [InlineData("/api/progress", StatusCodes.Status403Forbidden, true)]
    [InlineData("/git", StatusCodes.Status302Found, false)]
    [InlineData("/git", StatusCodes.Status302Found, true)]
    public async Task CookieEvents_ApiPathReturnsStatusOthersRedirect(string path, int expectedStatus, bool accessDenied)
    {
        // Arrange
        var options = _factory.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(IdentityConstants.ApplicationScheme);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = path;
        var scheme = new AuthenticationScheme(IdentityConstants.ApplicationScheme, null, typeof(CookieAuthenticationHandler));
        var redirectUri = "/somewhere";

        // Act
        if (accessDenied)
        {
            await options.Events.OnRedirectToAccessDenied(
                new RedirectContext<CookieAuthenticationOptions>(httpContext, scheme, options, new AuthenticationProperties(), redirectUri));
        }
        else
        {
            await options.Events.OnRedirectToLogin(
                new RedirectContext<CookieAuthenticationOptions>(httpContext, scheme, options, new AuthenticationProperties(), redirectUri));
        }

        // Assert
        Assert.Equal(expectedStatus, httpContext.Response.StatusCode);
        if (expectedStatus == StatusCodes.Status302Found)
        {
            Assert.Equal(redirectUri, httpContext.Response.Headers.Location.ToString());
        }
    }
}
