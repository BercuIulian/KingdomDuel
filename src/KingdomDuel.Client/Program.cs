using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using KingdomDuel.Client;
using KingdomDuel.Client.ApiClients;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// The client is hosted by the API, so the backend lives at the same origin.
// ReadResponseAsString = true is REQUIRED on Blazor WASM: the generated client
// otherwise deserializes the response stream synchronously, which the browser's
// HttpClient forbids (net_http_synchronous_reads_not_supported).
builder.Services.AddScoped(sp =>
    new MatchesApiClient(new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) })
    {
        ReadResponseAsString = true
    });

await builder.Build().RunAsync();
