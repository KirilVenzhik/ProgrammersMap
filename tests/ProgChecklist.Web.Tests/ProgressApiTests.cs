using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ProgChecklist.Web.Tests;

public class ProgressApiTests : IClassFixture<AppFactory>
{
    private const string GitKey = "git/init-add-commit";
    private const string JoinKey = "databases/join";
    private const string Api = "/api/progress";

    private readonly AppFactory _factory;

    public ProgressApiTests(AppFactory factory)
    {
        _factory = factory;
    }

    private HttpClient NewClient() => AuthTestHelpers.NewClient(_factory);

    private static HttpRequestMessage Request(HttpMethod method, string url, string? token, string? json = null)
    {
        var request = new HttpRequestMessage(method, url);
        if (token is not null)
        {
            request.Headers.Add("RequestVerificationToken", token);
        }

        if (json is not null)
        {
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        return request;
    }

    private static async Task<List<string>> KeysAsync(HttpClient client)
    {
        var response = await client.GetAsync(Api);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await ReadKeysAsync(response);
    }

    private static async Task<List<string>> ReadKeysAsync(HttpResponseMessage response)
    {
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("keys").EnumerateArray().Select(e => e.GetString()!).ToList();
    }

    private async Task<(HttpClient Client, string Token)> SignedInAsync()
    {
        var client = NewClient();
        await AuthTestHelpers.SignInNewUserAsync(_factory, client);
        var token = await AuthTestHelpers.GetCsrfTokenAsync(client);
        return (client, token);
    }

    private static string MergeBody(IEnumerable<string?> keys) => JsonSerializer.Serialize(new { keys });

    [Fact]
    public async Task Api_Guest_Returns401WithoutRedirectOnEveryEndpoint()
    {
        // Arrange
        using var client = NewClient();
        var requests = new[]
        {
            Request(HttpMethod.Get, Api, null),
            Request(HttpMethod.Put, $"{Api}/{GitKey}", "x"),
            Request(HttpMethod.Delete, $"{Api}/{GitKey}", "x"),
            Request(HttpMethod.Post, $"{Api}/merge", "x", "{\"keys\":[]}"),
            Request(HttpMethod.Delete, Api, "x"),
        };

        foreach (var request in requests)
        {
            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Null(response.Headers.Location);
        }
    }

    [Fact]
    public void AntiforgeryOptions_HeaderName_IsRequestVerificationToken()
    {
        // Arrange
        var options = _factory.Services.GetRequiredService<IOptions<AntiforgeryOptions>>().Value;

        // Assert
        Assert.Equal("RequestVerificationToken", options.HeaderName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("wrong-token")]
    public async Task Mutations_WithMissingOrWrongToken_Return400AndChangeNothing(string? token)
    {
        // Arrange
        var (client, goodToken) = await SignedInAsync();
        using var _ = client;
        Assert.Equal(HttpStatusCode.NoContent, (await client.SendAsync(Request(HttpMethod.Put, $"{Api}/{JoinKey}", goodToken))).StatusCode);
        var requests = new[]
        {
            Request(HttpMethod.Put, $"{Api}/{GitKey}", token),
            Request(HttpMethod.Delete, $"{Api}/{JoinKey}", token),
            Request(HttpMethod.Post, $"{Api}/merge", token, MergeBody([GitKey])),
            Request(HttpMethod.Delete, Api, token),
        };

        foreach (var request in requests)
        {
            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        Assert.Equal([JoinKey], await KeysAsync(client));
    }

    [Fact]
    public async Task Put_KnownTopic_Returns204AndIsIdempotent()
    {
        // Arrange
        var (client, token) = await SignedInAsync();
        using var _ = client;

        // Act
        var first = await client.SendAsync(Request(HttpMethod.Put, $"{Api}/{GitKey}", token));
        var second = await client.SendAsync(Request(HttpMethod.Put, $"{Api}/{GitKey}", token));
        var keys = await KeysAsync(client);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);
        Assert.Equal([GitKey], keys);
    }

    [Fact]
    public async Task Delete_KnownTopic_Returns204AndIsIdempotent()
    {
        // Arrange
        var (client, token) = await SignedInAsync();
        using var _ = client;
        await client.SendAsync(Request(HttpMethod.Put, $"{Api}/{GitKey}", token));

        // Act
        var first = await client.SendAsync(Request(HttpMethod.Delete, $"{Api}/{GitKey}", token));
        var second = await client.SendAsync(Request(HttpMethod.Delete, $"{Api}/{GitKey}", token));
        var keys = await KeysAsync(client);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);
        Assert.Empty(keys);
    }

    [Theory]
    [InlineData("git/no-such-topic")]
    [InlineData("nope/init-add-commit")]
    [InlineData("Git/init-add-commit")]
    public async Task PutAndDelete_UnknownOrCaseMismatchedTopic_Return404(string key)
    {
        // Arrange
        var (client, token) = await SignedInAsync();
        using var _ = client;

        // Act
        var put = await client.SendAsync(Request(HttpMethod.Put, $"{Api}/{key}", token));
        var delete = await client.SendAsync(Request(HttpMethod.Delete, $"{Api}/{key}", token));

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, put.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        Assert.Empty(await KeysAsync(client));
    }

    [Fact]
    public async Task Merge_WithExistingDuplicatesUnknownAndNull_ReturnsCleanUnion()
    {
        // Arrange
        var (client, token) = await SignedInAsync();
        using var _ = client;
        await client.SendAsync(Request(HttpMethod.Put, $"{Api}/{JoinKey}", token));

        // Act
        var response = await client.SendAsync(Request(HttpMethod.Post, $"{Api}/merge", token,
            MergeBody([GitKey, GitKey, "git/removed-topic", null, "garbage", JoinKey])));
        var keys = await ReadKeysAsync(response);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new[] { JoinKey, GitKey }.Order(StringComparer.Ordinal).ToList(), keys);
        Assert.Equal(keys, await KeysAsync(client));
    }

    [Fact]
    public async Task Merge_MoreThanLimit_Returns400AndStoresNothing()
    {
        // Arrange
        var (client, token) = await SignedInAsync();
        using var _ = client;
        var keys = Enumerable.Repeat(GitKey, 1001);

        // Act
        var response = await client.SendAsync(Request(HttpMethod.Post, $"{Api}/merge", token, MergeBody(keys)));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(await KeysAsync(client));
    }

    [Fact]
    public async Task Merge_ExactlyLimit_Returns200()
    {
        // Arrange
        var (client, token) = await SignedInAsync();
        using var _ = client;
        var keys = Enumerable.Repeat(GitKey, 1000);

        // Act
        var response = await client.SendAsync(Request(HttpMethod.Post, $"{Api}/merge", token, MergeBody(keys)));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal([GitKey], await ReadKeysAsync(response));
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("")]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("{\"keys\":null}")]
    [InlineData("{\"keys\":\"git\"}")]
    [InlineData("{\"keys\":[1,2]}")]
    public async Task Merge_InvalidBody_Returns400(string json)
    {
        // Arrange
        var (client, token) = await SignedInAsync();
        using var _ = client;

        // Act
        var response = await client.SendAsync(Request(HttpMethod.Post, $"{Api}/merge", token, json));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Merge_OversizedBody_ReturnsPayloadTooLargeOrBadRequest()
    {
        // Arrange
        var (client, token) = await SignedInAsync();
        using var _ = client;
        var json = "{\"keys\":[\"" + new string('a', 200_000) + "\"]}";

        // Act
        var response = await client.SendAsync(Request(HttpMethod.Post, $"{Api}/merge", token, json));

        // Assert
        Assert.True(
            response.StatusCode is HttpStatusCode.RequestEntityTooLarge or HttpStatusCode.BadRequest,
            $"unexpected {response.StatusCode}");
        Assert.Empty(await KeysAsync(client));
    }

    [Fact]
    public async Task DeleteAll_Returns204AndClearsProgress()
    {
        // Arrange
        var (client, token) = await SignedInAsync();
        using var _ = client;
        await client.SendAsync(Request(HttpMethod.Put, $"{Api}/{GitKey}", token));
        await client.SendAsync(Request(HttpMethod.Put, $"{Api}/{JoinKey}", token));

        // Act
        var response = await client.SendAsync(Request(HttpMethod.Delete, Api, token));

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(await KeysAsync(client));
    }

    [Fact]
    public async Task Progress_TwoUsers_AreIsolated()
    {
        // Arrange
        var (clientA, tokenA) = await SignedInAsync();
        var (clientB, tokenB) = await SignedInAsync();
        using var a = clientA;
        using var b = clientB;

        // Act
        await clientA.SendAsync(Request(HttpMethod.Put, $"{Api}/{GitKey}", tokenA));
        var keysB = await KeysAsync(clientB);
        await clientB.SendAsync(Request(HttpMethod.Put, $"{Api}/{JoinKey}", tokenB));
        await clientB.SendAsync(Request(HttpMethod.Delete, Api, tokenB));

        // Assert
        Assert.Empty(keysB);
        Assert.Equal([GitKey], await KeysAsync(clientA));
        Assert.Empty(await KeysAsync(clientB));
    }

    [Fact]
    public async Task Responses_HaveNoStoreNoIndexAndJsonContentType()
    {
        // Arrange
        var (client, token) = await SignedInAsync();
        using var _ = client;
        using var guest = NewClient();

        // Act
        var get = await client.GetAsync(Api);
        var put = await client.SendAsync(Request(HttpMethod.Put, $"{Api}/{GitKey}", token));
        var unauthorized = await guest.GetAsync(Api);

        // Assert
        Assert.Equal("application/json", get.Content.Headers.ContentType?.MediaType);
        foreach (var response in new[] { get, put, unauthorized })
        {
            Assert.Contains("no-store", string.Join(",", response.Headers.GetValues("Cache-Control")));
            Assert.Equal("noindex", Assert.Single(response.Headers.GetValues("X-Robots-Tag")));
        }
    }

    [Fact]
    public async Task Get_Json_UsesCamelCaseKeysProperty()
    {
        // Arrange
        var (client, token) = await SignedInAsync();
        using var _ = client;
        await client.SendAsync(Request(HttpMethod.Put, $"{Api}/{GitKey}", token));

        // Act
        var body = await client.GetStringAsync(Api);

        // Assert
        Assert.Equal("{\"keys\":[\"git/init-add-commit\"]}", body);
    }

    [Fact]
    public async Task Layout_SignedIn_HasCsrfAndUserIdMeta()
    {
        // Arrange
        using var client = NewClient();
        var (_, userId) = await AuthTestHelpers.SignInNewUserAsync(_factory, client);

        // Act
        var html = await client.GetStringAsync("/");

        // Assert
        Assert.Contains("<meta name=\"csrf-token\" content=\"", html);
        Assert.Contains($"<meta name=\"user-id\" content=\"{userId}\">", html);
    }

    [Fact]
    public async Task Layout_Guest_HasNoCsrfOrUserIdMeta()
    {
        // Arrange
        using var client = NewClient();

        // Act
        var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.DoesNotContain("csrf-token", html);
        Assert.DoesNotContain("name=\"user-id\"", html);
        Assert.False(response.Headers.TryGetValues("Set-Cookie", out var cookies) && cookies.Any(c => c.Contains("Antiforgery")));
    }

    [Fact]
    public async Task Sitemap_DoesNotContainApi()
    {
        // Arrange
        using var client = NewClient();

        // Act
        var xml = await client.GetStringAsync("/sitemap.xml");

        // Assert
        Assert.DoesNotContain("/api/", xml);
    }
}
