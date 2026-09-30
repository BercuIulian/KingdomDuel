using System.Net;
using System.Text;
using System.Text.Json;
using Bunit;
using KingdomDuel.Client.ApiClients;
using KingdomDuel.Client.Authentication;
using KingdomDuel.Shared.Requests.Auth;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace KingdomDuel.Client.Tests.Authentication;

public sealed class AuthenticationServiceTests : IDisposable
{
    private readonly BunitContext context = new();
    private readonly FakeServer server = new();

    public AuthenticationServiceTests()
    {
        context.JSInterop.Setup<string?>("localStorage.getItem", _ => true).SetResult(null);
        context.JSInterop.SetupVoid("localStorage.setItem", _ => true).SetVoidResult();
        context.JSInterop.SetupVoid("localStorage.removeItem", _ => true).SetVoidResult();
    }

    private NavigationManager Navigation => context.Services.GetRequiredService<NavigationManager>();

    public void Dispose() => context.Dispose();

    [Fact]
    public async Task A_successful_login_keeps_the_token()
    {
        var token = Tokens.Fresh();
        server.Respond(HttpStatusCode.OK, new
        {
            token,
            expiresAt = DateTimeOffset.UtcNow.AddDays(7),
            user = new { id = Guid.NewGuid(), userName = "rege", createdAt = DateTimeOffset.UtcNow },
        });

        var error = await Service().LoginAsync(new LoginRequest { UserName = "rege", Password = "parola-buna" });

        error.ShouldBeNull();
        context.JSInterop.VerifyInvoke("localStorage.setItem").Arguments[1].ShouldBe(token);
    }

    [Fact]
    public async Task A_refused_login_returns_the_server_reason()
    {
        server.Respond(HttpStatusCode.BadRequest, new { errors = new { generalErrors = new[] { "Nume de utilizator sau parolă greșite." } } });

        var error = await Service().LoginAsync(new LoginRequest { UserName = "rege", Password = "gresit" });

        error.ShouldBe("Nume de utilizator sau parolă greșite.");
    }

    [Fact]
    public async Task A_refused_registration_without_details_gets_a_plain_message()
    {
        server.Respond(HttpStatusCode.BadRequest, new { });

        var error = await Service().RegisterAsync(new RegisterRequest { UserName = "rege", Password = "parola-buna" });

        error.ShouldBe("Contul nu a putut fi creat.");
    }

    [Fact]
    public async Task An_unreachable_server_is_said_so()
    {
        server.Fail();

        var error = await Service().LoginAsync(new LoginRequest { UserName = "rege", Password = "parola-buna" });

        error.ShouldBe("Nu am putut contacta serverul.");
    }

    [Fact]
    public async Task Logging_out_signs_out_here_even_when_the_server_cannot_be_reached()
    {
        server.Fail();
        Navigation.NavigateTo("/play");

        await Service().LogoutAsync();

        context.JSInterop.VerifyInvoke("localStorage.removeItem");
        Navigation.Uri.ShouldEndWith("/login");
    }

    private AuthenticationService Service()
    {
        var api = new ApiClient(new HttpClient(server) { BaseAddress = new Uri("http://localhost/") })
        {
            ReadResponseAsString = true,
        };

        var authentication = new TokenAuthenticationStateProvider(new TokenStore(context.JSInterop.JSRuntime));

        return new AuthenticationService(api, authentication, Navigation);
    }

    private sealed class FakeServer : HttpMessageHandler
    {
        private HttpStatusCode status = HttpStatusCode.OK;
        private string body = "{}";
        private bool down;

        public void Respond(HttpStatusCode code, object payload)
        {
            status = code;
            body = JsonSerializer.Serialize(payload);
        }

        public void Fail() => down = true;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            if (down)
            {
                throw new HttpRequestException("Serverul nu răspunde.");
            }

            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            });
        }
    }
}
