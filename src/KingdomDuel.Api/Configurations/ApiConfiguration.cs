using System.Text.Json.Serialization;
using FastEndpoints;
using FastEndpoints.Swagger;
using KingdomDuel.Application;

namespace KingdomDuel.Api.Configurations;

public static class ApiConfiguration
{
    public static WebApplicationBuilder AddApiConfiguration(this WebApplicationBuilder builder)
    {
        var fastEndpoints = builder.Services.AddFastEndpoints(o =>
        {
            o.Assemblies = [typeof(AppDbContext).Assembly];
            o.IncludeAbstractValidators = true;
        });

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
