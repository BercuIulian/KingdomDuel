using System.Net;
using Bunit;
using KingdomDuel.Client.Authentication;
using KingdomDuel.Shared.Auth;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace KingdomDuel.Client.Tests.Authentication;

public sealed class TokenFlowTests : IDisposable
{
    private const string StorageKey = "kingdomduel.token";

    private readonly BunitContext context = new();

    public TokenFlowTests()
    {
        context.JSInterop.SetupVoid("localStorage.setItem", _ => true).SetVoidResult();
        context.JSInterop.SetupVoid("localStorage.removeItem", _ => true).SetVoidResult();
    }

    private NavigationManager Navigation => context.Services.GetRequiredService<NavigationManager>();

    public void Dispose() => context.Dispose();

    [Fact]
    public async Task The_token_is_read_from_storage_once_and_then_kept()
    {
        var saved = Tokens.Fresh();
        Stored(saved);
        var store = new TokenStore(context.JSInterop.JSRuntime);

        (await store.GetAsync()).ShouldBe(saved);
        (await store.GetAsync()).ShouldBe(saved);

        context.JSInterop.Invocations.Count(i => i.Identifier == "localStorage.getItem").ShouldBe(1);
    }

    [Fact]
    public async Task Saving_and_clearing_go_through_to_storage()
    {
        Stored(null);
        var store = new TokenStore(context.JSInterop.JSRuntime);

        await store.SetAsync("abc");
        (await store.GetAsync()).ShouldBe("abc");

        await store.ClearAsync();
        (await store.GetAsync()).ShouldBeNull();

        context.JSInterop.VerifyInvoke("localStorage.setItem").Arguments.ShouldBe([StorageKey, "abc"]);
        context.JSInterop.VerifyInvoke("localStorage.removeItem");
    }

    [Fact]
    public async Task A_stored_valid_token_signs_the_player_in()
    {
        Stored(Tokens.Fresh("regele.mihai"));

        var state = await Provider().GetAuthenticationStateAsync();

        state.User.Identity!.IsAuthenticated.ShouldBeTrue();
        state.User.Identity.Name.ShouldBe("regele.mihai");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("expired")]
    public async Task No_token_or_an_expired_one_leaves_the_player_anonymous(string? kind)
    {
        Stored(kind is null ? null : Tokens.Expired());

        var state = await Provider().GetAuthenticationStateAsync();

        state.User.Identity!.IsAuthenticated.ShouldBeFalse();
    }

    [Fact]
    public async Task Signing_in_and_out_is_announced()
    {
        Stored(null);
        var provider = Provider();
        var announced = new List<bool>();
        provider.AuthenticationStateChanged += async task => announced.Add((await task).User.Identity!.IsAuthenticated);

        await provider.SignInAsync(Tokens.Fresh());
        await provider.SignOutAsync();

        announced.ShouldBe([true, false]);
    }

    [Fact]
    public async Task Requests_carry_a_valid_token()
    {
        Stored(Tokens.Fresh());
        var server = new FakeServer();

        await Send(server);

        server.Last!.Headers.Authorization!.Scheme.ShouldBe("Bearer");
    }

    [Fact]
    public async Task An_expired_token_is_not_sent()
    {
        Stored(Tokens.Expired());
        var server = new FakeServer();

        await Send(server);

        server.Last!.Headers.Authorization.ShouldBeNull();
    }

    [Fact]
    public async Task A_renewed_token_from_the_server_replaces_the_old_one()
    {
        Stored(Tokens.Fresh());
        var renewed = Tokens.Fresh("regele.nou");
        var server = new FakeServer(response => response.Headers.Add(AuthHeaders.RenewedToken, renewed));

        await Send(server);

        context.JSInterop.VerifyInvoke("localStorage.setItem").Arguments.ShouldBe([StorageKey, renewed]);
    }

    [Fact]
    public async Task A_rejected_token_signs_out_and_sends_to_login_with_the_way_back()
    {
        Stored(Tokens.Fresh());
        Navigation.NavigateTo("/game?code=ABC123");
        var server = new FakeServer(status: HttpStatusCode.Unauthorized);

        await Send(server);

        context.JSInterop.VerifyInvoke("localStorage.removeItem");
        Navigation.Uri.ShouldEndWith("/login?returnUrl=%2Fgame%3Fcode%3DABC123");
    }

    [Fact]
    public async Task A_rejection_on_the_login_page_stays_there()
    {
        Stored(null);
        Navigation.NavigateTo("/login?returnUrl=%2Fplay");
        var server = new FakeServer(status: HttpStatusCode.Unauthorized);

        await Send(server);

        Navigation.Uri.ShouldEndWith("/login?returnUrl=%2Fplay");
    }

    private void Stored(string? token) =>
        context.JSInterop.Setup<string?>("localStorage.getItem", StorageKey).SetResult(token);

    private TokenAuthenticationStateProvider Provider(TokenStore? store = null) =>
        new(store ?? new TokenStore(context.JSInterop.JSRuntime));

    private async Task Send(FakeServer server)
    {
        var store = new TokenStore(context.JSInterop.JSRuntime);
        var handler = new BearerTokenHandler(store, Provider(store), Navigation) { InnerHandler = server };

        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        using var _ = await client.GetAsync("/api/auth/profile", Xunit.TestContext.Current.CancellationToken);
    }

    private sealed class FakeServer(
        Action<HttpResponseMessage>? shape = null,
        HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
    {
        public HttpRequestMessage? Last { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Last = request;

            var response = new HttpResponseMessage(status);
            shape?.Invoke(response);

            return Task.FromResult(response);
        }
    }
}
