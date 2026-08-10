using System.Text.Json.Serialization;
using FastEndpoints;
using FastEndpoints.Swagger;
using KingdomDuel.Application;

namespace KingdomDuel.Api.Configurations;

public static class ApiConfiguration
{
    // CORS policy that lets the Blazor WASM client (served from a different
    // origin) call the API during development.
    private const string ClientCorsPolicy = "ClientApp";

    public static WebApplicationBuilder AddApiConfiguration(this WebApplicationBuilder builder)
    {
        var fastEndpoints = builder.Services.AddFastEndpoints(o =>
        {
            o.Assemblies = [typeof(AppDbContext).Assembly];
            o.IncludeAbstractValidators = true;
        });

        // Dev-only: the client runs on its own dev-server port, so it needs CORS.
        if (!builder.Environment.IsProduction())
        {
            builder.Services.AddCors(o => o.AddPolicy(ClientCorsPolicy, p => p
                .WithOrigins("http://localhost:5047", "https://localhost:7072")
                .AllowAnyHeader()
                .AllowAnyMethod()));
        }

        // OpenAPI document - dev only. It is what the NSwag client generator consumes.
        if (!builder.Environment.IsProduction())
        {
            fastEndpoints.SwaggerDocument(o =>
            {
                o.ShortSchemaNames = true;
                o.DocumentSettings = s =>
                {
                    s.DocumentName = ClientGenerationConfiguration.DocumentName;
                    s.Title = "Kingdom Duel API";
                    s.Version = "v1";
                };
            });
        }

        return builder;
    }

    public static void UseApiConfiguration(this WebApplication app)
    {
        if (!app.Environment.IsProduction())
        {
            app.UseCors(ClientCorsPolicy);
        }

        app.UseAuthConfiguration();

        app.UseFastEndpoints(c =>
        {
            c.Endpoints.RoutePrefix = "api";
            c.Serializer.Options.Converters.Add(new JsonStringEnumConverter());
        });

        if (!app.Environment.IsProduction())
        {
            app.UseSwaggerGen();
        }
    }
}
