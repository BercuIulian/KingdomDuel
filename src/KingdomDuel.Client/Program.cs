using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using KingdomDuel.Client;
using KingdomDuel.Client.ApiClients;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddScoped(sp =>
    new ApiClient(new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) })
    {
        ReadResponseAsString = true
    });

await builder.Build().RunAsync();
