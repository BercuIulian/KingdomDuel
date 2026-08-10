using KingdomDuel.Api.Configurations;

var builder = WebApplication.CreateBuilder(args);

builder.AddApiConfiguration();
builder.AddAuthConfiguration();
builder.AddDatabaseConfiguration();

var app = builder.Build();

// Serve the hosted Blazor WASM client (its static assets + framework files).
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseApiConfiguration();

// If invoked with `--generateclients true`, writes the API client and exits (no DB needed).
await app.GenerateApiClientAndExitAsync();

await app.ApplyDatabaseMigrations();

// Any non-API, non-file route falls back to the SPA so client-side routing works.
app.MapFallbackToFile("index.html");

await app.RunAsync();
