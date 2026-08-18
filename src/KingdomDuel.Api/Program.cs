using KingdomDuel.Api.Configurations;

var builder = WebApplication.CreateBuilder(args);

builder.AddProxyConfiguration();
builder.AddApiConfiguration();
builder.AddAuthConfiguration();
builder.AddRateLimitConfiguration();
builder.AddDatabaseConfiguration();
builder.AddHealthConfiguration();

var app = builder.Build();

app.UseForwardedHeaders();

app.UseExceptionConfiguration();

app.MapStaticAssets().DisableRateLimiting();

app.UseApiConfiguration();

// If invoked with `--generateclients true`, writes the API client and exits (no DB needed).
await app.GenerateApiClientAndExitAsync();

await app.ApplyDatabaseMigrations();

app.MapHealthEndpoints();

// Any non-API, non-file route falls back to the SPA so client-side routing works.
app.MapFallbackToFile("index.html").DisableRateLimiting();

await app.RunAsync();
