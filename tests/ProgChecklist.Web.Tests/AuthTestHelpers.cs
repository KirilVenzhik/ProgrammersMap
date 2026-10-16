using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ProgChecklist.Data;

namespace ProgChecklist.Web.Tests;

internal static class AuthTestHelpers
{
    public const string GoodPassword = "correct horse";

    public static string NewEmail() => $"u{Guid.NewGuid():N}@example.test";

    public static HttpClient NewClient(AppFactory factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    public static async Task<string> GetTokenAsync(HttpClient client, string path)
    {
        var html = await client.GetStringAsync(path);
        var match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(match.Success, "antiforgery token not found");
        return match.Groups[1].Value;
    }

    public static async Task<string> CreateUserAsync(AppFactory factory, string? email = null)
    {
        email ??= NewEmail();
        using var scope = factory.Services.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>()
            .CreateAsync(new AppUser { UserName = email, Email = email }, GoodPassword);
        Assert.True(result.Succeeded);
        return email;
    }

    public static async Task<HttpResponseMessage> LoginAsync(
        HttpClient client, string email, string password, bool remember = false, string? returnUrl = null)
    {
        var fields = new Dictionary<string, string>
        {
            ["Input.Email"] = email,
            ["Input.Password"] = password,
            ["__RequestVerificationToken"] = await GetTokenAsync(client, "/account/login"),
        };
        if (remember)
        {
            fields["Input.RememberMe"] = "true";
        }

        if (returnUrl is not null)
        {
            fields["ReturnUrl"] = returnUrl;
        }

        return await client.PostAsync("/account/login", new FormUrlEncodedContent(fields));
    }

    public static string? AuthCookie(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values.FirstOrDefault(c => c.StartsWith(".ProgChecklist.Auth=") && !c.StartsWith(".ProgChecklist.Auth=;"))
            : null;

    public static async Task<string> GetCsrfTokenAsync(HttpClient client)
    {
        var html = await client.GetStringAsync("/");
        var match = Regex.Match(html, "<meta name=\"csrf-token\" content=\"([^\"]+)\"");
        Assert.True(match.Success, "csrf-token meta not found");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    /// <summary>Creates a user, signs the client in and returns (email, userId).</summary>
    public static async Task<(string Email, string UserId)> SignInNewUserAsync(AppFactory factory, HttpClient client)
    {
        var email = await CreateUserAsync(factory);
        string userId;
        using (var scope = factory.Services.CreateScope())
        {
            userId = (await scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>().FindByEmailAsync(email))!.Id;
        }

        var response = await LoginAsync(client, email, GoodPassword);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        return (email, userId);
    }
}
