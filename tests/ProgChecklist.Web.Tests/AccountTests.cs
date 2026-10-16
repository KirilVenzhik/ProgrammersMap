using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProgChecklist.Data;

namespace ProgChecklist.Web.Tests;

public class AccountTests : IClassFixture<AppFactory>
{
    private const string GitKey = "git/init-add-commit";
    private const string JoinKey = "databases/join";
    private const string NewPassword = "brand new pass";

    private readonly AppFactory _factory;

    public AccountTests(AppFactory factory)
    {
        _factory = factory;
    }

    private HttpClient NewClient() => AuthTestHelpers.NewClient(_factory);

    private async Task<(HttpClient Client, string Email, string UserId)> SignedInAsync()
    {
        var client = NewClient();
        var (email, userId) = await AuthTestHelpers.SignInNewUserAsync(_factory, client);
        return (client, email, userId);
    }

    private async Task MarkAsync(string userId, params string[] keys)
    {
        using var scope = _factory.Services.CreateScope();
        var progress = scope.ServiceProvider.GetRequiredService<IProgressService>();
        foreach (var key in keys)
        {
            Assert.True(await progress.MarkAsync(userId, key));
        }
    }

    private static async Task<HttpResponseMessage> PostAsync(HttpClient client, string handler, Dictionary<string, string> fields)
    {
        fields["__RequestVerificationToken"] = await AuthTestHelpers.GetTokenAsync(client, "/account");
        return await client.PostAsync($"/account?handler={handler}", new FormUrlEncodedContent(fields));
    }

    private static Task<HttpResponseMessage> ChangePasswordAsync(HttpClient client, string current, string next, string confirm) =>
        PostAsync(client, "ChangePassword", new Dictionary<string, string>
        {
            ["ChangePassword.CurrentPassword"] = current,
            ["ChangePassword.NewPassword"] = next,
            ["ChangePassword.ConfirmPassword"] = confirm,
        });

    private static Task<HttpResponseMessage> DeleteAsync(HttpClient client, string password) =>
        PostAsync(client, "Delete", new Dictionary<string, string> { ["Delete.Password"] = password });

    // The test server may report the redirect as an absolute URL.
    private static string PathAndQuery(Uri location) =>
        location.IsAbsoluteUri ? location.PathAndQuery : location.OriginalString;

    private async Task<bool> UserExistsAsync(string userId)
    {
        using var scope = _factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().Users.AnyAsync(u => u.Id == userId);
    }

    private async Task<int> ProgressRowsAsync(string userId)
    {
        using var scope = _factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().TopicProgress.CountAsync(p => p.UserId == userId);
    }

    [Fact]
    public async Task Get_AccountAsGuest_RedirectsToLoginWithReturnUrl()
    {
        // Arrange
        using var client = NewClient();

        // Act
        var response = await client.GetAsync("/account");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/account/login?returnUrl=%2Faccount", PathAndQuery(response.Headers.Location!));
    }

    [Fact]
    public async Task Login_WithAccountReturnUrl_LandsOnAccount()
    {
        // Arrange
        using var client = NewClient();
        var email = await AuthTestHelpers.CreateUserAsync(_factory);
        var location = PathAndQuery((await client.GetAsync("/account")).Headers.Location!);
        var returnUrl = Uri.UnescapeDataString(location[(location.IndexOf("returnUrl=", StringComparison.Ordinal) + "returnUrl=".Length)..]);

        // Act
        var login = await AuthTestHelpers.LoginAsync(client, email, AuthTestHelpers.GoodPassword, returnUrl: returnUrl);
        var page = await client.GetAsync(login.Headers.Location);

        // Assert
        Assert.Equal("/account", login.Headers.Location?.OriginalString);
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        Assert.Contains(email, await page.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Get_AccountSignedIn_ShowsEmailDateCountAndNoIndex()
    {
        // Arrange
        var (client, email, userId) = await SignedInAsync();
        await MarkAsync(userId, GitKey, JoinKey);

        // Act
        var response = await client.GetAsync("/account");
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<title>Аккаунт — Карта программиста</title>", body);
        Assert.Contains("<meta name=\"robots\" content=\"noindex\">", body);
        Assert.Contains($"<dd>{email}</dd>", body);
        Assert.Contains("<dd>2 темы</dd>", body);
        Assert.Matches(new Regex(@"<dd>\d{1,2} [а-я]+ " + DateTime.UtcNow.Year + "</dd>"), body);
        Assert.Contains("Аккаунт и весь прогресс будут удалены без возможности восстановления.", body);
        Assert.Contains("href=\"/account\"", body);
    }

    [Fact]
    public async Task Export_SignedIn_ReturnsOnlyOwnDataAsJsonAttachment()
    {
        // Arrange
        var (client, email, userId) = await SignedInAsync();
        var (_, otherId) = await AuthTestHelpers.SignInNewUserAsync(_factory, NewClient());
        await MarkAsync(userId, GitKey);
        await MarkAsync(otherId, JoinKey);
        string hash;
        using (var scope = _factory.Services.CreateScope())
        {
            hash = (await scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>().FindByIdAsync(userId))!.PasswordHash!;
        }

        // Act
        var response = await client.GetAsync("/account?handler=Export");
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType?.ToString());
        Assert.True(response.Headers.CacheControl?.NoStore);
        var disposition = response.Content.Headers.ContentDisposition;
        Assert.Equal("attachment", disposition?.DispositionType);
        Assert.Equal("progchecklist-data.json", disposition?.FileName?.Trim('"'));
        using var doc = JsonDocument.Parse(body);
        Assert.Equal(email, doc.RootElement.GetProperty("email").GetString());
        Assert.Matches(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}Z$", doc.RootElement.GetProperty("createdAt").GetString()!);
        var progress = doc.RootElement.GetProperty("progress").EnumerateArray().ToList();
        var entry = Assert.Single(progress);
        Assert.Equal(GitKey, entry.GetProperty("topicKey").GetString());
        Assert.Matches(@"Z$", entry.GetProperty("completedAt").GetString()!);
        Assert.DoesNotContain(JoinKey, body);
        Assert.DoesNotContain(otherId, body);
        Assert.DoesNotContain(userId, body);
        Assert.DoesNotContain(hash, body);
        foreach (var forbidden in new[] { "PasswordHash", "SecurityStamp", "ConcurrencyStamp", "Normalized", "Lockout" })
        {
            Assert.DoesNotContain(forbidden, body, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task Export_AsGuest_RedirectsToLoginWithoutData()
    {
        // Arrange
        using var client = NewClient();

        // Act
        var response = await client.GetAsync("/account?handler=Export");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("/account/login", PathAndQuery(response.Headers.Location!));
    }

    [Fact]
    public async Task ChangePassword_WrongCurrent_ShowsErrorAndKeepsOldPassword()
    {
        // Arrange
        var (client, email, _) = await SignedInAsync();

        // Act
        var response = await ChangePasswordAsync(client, "not the password", NewPassword, NewPassword);
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Неверный пароль.", body);
        var relogin = await AuthTestHelpers.LoginAsync(NewClient(), email, AuthTestHelpers.GoodPassword);
        Assert.Equal(HttpStatusCode.Redirect, relogin.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_ConfirmationMismatch_ShowsError()
    {
        // Arrange
        var (client, email, _) = await SignedInAsync();

        // Act
        var response = await ChangePasswordAsync(client, AuthTestHelpers.GoodPassword, NewPassword, "something else");
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Пароли не совпадают.", body);
        var relogin = await AuthTestHelpers.LoginAsync(NewClient(), email, AuthTestHelpers.GoodPassword);
        Assert.Equal(HttpStatusCode.Redirect, relogin.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_TooShortNew_ShowsError()
    {
        // Arrange
        var (client, _, _) = await SignedInAsync();

        // Act
        var response = await ChangePasswordAsync(client, AuthTestHelpers.GoodPassword, "short", "short");
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("не менее 8 символов", body);
    }

    [Fact]
    public async Task ChangePassword_Valid_ShowsStatusAndSwitchesPassword()
    {
        // Arrange
        var (client, email, _) = await SignedInAsync();

        // Act
        var response = await ChangePasswordAsync(client, AuthTestHelpers.GoodPassword, NewPassword, NewPassword);
        var page = await client.GetAsync(response.Headers.Location);
        var body = await page.Content.ReadAsStringAsync();
        var again = await client.GetAsync("/account");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        Assert.Contains("Пароль изменён.", body);
        Assert.DoesNotContain("Пароль изменён.", await again.Content.ReadAsStringAsync());
        var withNew = await AuthTestHelpers.LoginAsync(NewClient(), email, NewPassword);
        var withOld = await AuthTestHelpers.LoginAsync(NewClient(), email, AuthTestHelpers.GoodPassword);
        Assert.Equal(HttpStatusCode.Redirect, withNew.StatusCode);
        Assert.Equal(HttpStatusCode.OK, withOld.StatusCode);
    }

    [Fact]
    public async Task Delete_WrongPassword_KeepsUserAndProgress()
    {
        // Arrange
        var (client, _, userId) = await SignedInAsync();
        await MarkAsync(userId, GitKey);

        // Act
        var response = await DeleteAsync(client, "not the password");
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Неверный пароль.", body);
        Assert.DoesNotContain("clear-progress", body);
        Assert.True(await UserExistsAsync(userId));
        Assert.Equal(1, await ProgressRowsAsync(userId));
    }

    [Fact]
    public async Task Delete_CorrectPassword_RemovesUserAndProgressAndSignsOut()
    {
        // Arrange
        var (client, _, userId) = await SignedInAsync();
        await MarkAsync(userId, GitKey, JoinKey);

        // Act
        var response = await DeleteAsync(client, AuthTestHelpers.GoodPassword);
        var home = await client.GetAsync(response.Headers.Location);
        var homeBody = await home.Content.ReadAsStringAsync();
        var later = await (await client.GetAsync("/")).Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.OriginalString);
        Assert.Contains(response.Headers.GetValues("Set-Cookie"),
            c => c.StartsWith(IdentityServiceCollectionExtensions.AuthCookieName + "=;") && c.Contains("expires=", StringComparison.OrdinalIgnoreCase));
        Assert.False(await UserExistsAsync(userId));
        Assert.Equal(0, await ProgressRowsAsync(userId));
        Assert.Contains("href=\"/account/login", homeBody);
        Assert.Single(Regex.Matches(homeBody, "<meta name=\"clear-progress\" content=\"1\">"));
        Assert.DoesNotContain("clear-progress", later);
    }

    [Fact]
    public async Task Api_AfterAccountDeletedElsewhere_Returns401NotServerError()
    {
        // Arrange
        var (client, email, userId) = await SignedInAsync();
        var token = await AuthTestHelpers.GetCsrfTokenAsync(client);
        using var other = NewClient();
        await AuthTestHelpers.LoginAsync(other, email, AuthTestHelpers.GoodPassword);
        var deleted = await DeleteAsync(other, AuthTestHelpers.GoodPassword);
        Assert.Equal(HttpStatusCode.Redirect, deleted.StatusCode);
        var put = new HttpRequestMessage(HttpMethod.Put, $"/api/progress/{GitKey}");
        put.Headers.Add("RequestVerificationToken", token);

        // Act
        var get = await client.GetAsync("/api/progress");
        var putResponse = await client.SendAsync(put);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, get.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, putResponse.StatusCode);
        Assert.False(await UserExistsAsync(userId));
    }
}
