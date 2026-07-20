using KingdomDuel.Api.Configurations;

var builder = WebApplication.CreateBuilder(args);

builder.AddApiConfiguration();
builder.AddDatabaseConfiguration();

var app = builder.Build();

app.UseApiConfiguration();

// If invoked with `--generateclients true`, writes the API client and exits (no DB needed).
await app.GenerateApiClientAndExitAsync();

await app.ApplyDatabaseMigrations();

app.MapGet("/", () => Results.Ok(new { status = "Kingdom Duel API", version = "0.1.0" }));

await app.RunAsync();
