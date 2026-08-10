using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using KingdomDuel.Client;
using KingdomDuel.Client.ApiClients;
using KingdomDuel.Client.Authentication;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<TokenStore>();
builder.Services.AddScoped<TokenAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(
    sp => sp.GetRequiredService<TokenAuthenticationStateProvider>());
builder.Services.AddScoped<BearerTokenHandler>();
builder.Services.AddScoped<AuthenticationService>();

// The API client goes through BearerTokenHandler, which attaches the token, swaps in a
// renewed one when the server sends it back, and signs out on 401.
builder.Services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<BearerTokenHandler>();
    handler.InnerHandler = new HttpClientHandler();

    var http = new HttpClient(handler) { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };

    return new ApiClient(http) { ReadResponseAsString = true };
});

await builder.Build().RunAsync();
