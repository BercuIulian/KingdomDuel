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

// Serve the hosted Blazor WASM client (its static assets + framework files).
// Scoase de sub rate limiting: o incarcare proaspata cere ~79 de resurse in
// productie si peste 200 in dev, iar limita globala e 90 pe minut. Contorizate,
// fisierele statice epuizeaza singure bugetul si pagina primeste 429 in timp ce
// se incarca - vizibil mai ales in incognito, unde nu exista nici cache, nici
// token, deci partitia e pe IP si se imparte intre cei din spatele unui NAT.
app.MapStaticAssets().DisableRateLimiting();

app.UseApiConfiguration();

// If invoked with `--generateclients true`, writes the API client and exits (no DB needed).
await app.GenerateApiClientAndExitAsync();

await app.ApplyDatabaseMigrations();

app.MapHealthEndpoints();

// Any non-API, non-file route falls back to the SPA so client-side routing works.
app.MapFallbackToFile("index.html").DisableRateLimiting();

await app.RunAsync();
