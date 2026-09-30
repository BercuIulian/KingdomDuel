using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using KingdomDuel.Shared.Requests.Auth;
using KingdomDuel.Shared.Responses.Auth;

namespace KingdomDuel.IntegrationTests;

public sealed class Player
{
    public const string Password = "parola-de-test";

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly HttpClient http;

    public Player(HttpClient http, string userName, string ip)
    {
        this.http = http;
        UserName = userName;
        http.DefaultRequestHeaders.Add("X-Forwarded-For", ip);
    }

    public string UserName { get; }

    public string? Token { get; private set; }

    public async Task<Player> RegisterAsync()
    {
        var auth = await SendAsync<AuthResponse>(
            HttpMethod.Post,
            "/api/auth/register",
            new RegisterRequest { UserName = UserName, Password = Password });

        UseToken(auth.Token);

        return this;
    }

    public void UseToken(string? token)
    {
        Token = token;
        http.DefaultRequestHeaders.Authorization = token is null ? null : new AuthenticationHeaderValue("Bearer", token);
    }

    public Task<T> GetAsync<T>(string path) => SendAsync<T>(HttpMethod.Get, path);

    public Task<T> PostAsync<T>(string path, object? body = null) => SendAsync<T>(HttpMethod.Post, path, body);

    public Task<T> PutAsync<T>(string path, object body) => SendAsync<T>(HttpMethod.Put, path, body);

    public async Task<T> SendAsync<T>(HttpMethod method, string path, object? body = null)
    {
        using var response = await SendRawAsync(method, path, body);

        if (!response.IsSuccessStatusCode)
        {
            var text = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

            throw new ShouldAssertException($"{method} {path} a răspuns {(int)response.StatusCode}: {text}");
        }

        return (await response.Content.ReadFromJsonAsync<T>(Json, TestContext.Current.CancellationToken))!;
    }

    public async Task<ApiError> FailAsync(
        HttpMethod method,
        string path,
        object? body = null,
        HttpStatusCode expected = HttpStatusCode.BadRequest)
    {
        using var response = await SendRawAsync(method, path, body);
        var text = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(expected, text);

        return string.IsNullOrWhiteSpace(text)
            ? new ApiError()
            : JsonSerializer.Deserialize<ApiError>(text, Json)!;
    }

    public async Task<HttpResponseMessage> SendRawAsync(HttpMethod method, string path, object? body = null)
    {
        using var request = new HttpRequestMessage(method, path);

        if (body is not null)
        {
            request.Content = JsonContent.Create(body, body.GetType(), options: Json);
        }

        return await http.SendAsync(request, TestContext.Current.CancellationToken);
    }
}

public sealed class ApiError
{
    public int StatusCode { get; set; }

    public string? Message { get; set; }

    public Dictionary<string, List<string>> Errors { get; set; } = [];

    public IEnumerable<string> Messages => Errors.Values.SelectMany(m => m).Append(Message ?? string.Empty);
}
