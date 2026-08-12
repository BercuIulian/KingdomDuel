using KingdomDuel.Application;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace KingdomDuel.Api.Configurations;

public static class HealthConfiguration
{
    private const string ReadyTag = "ready";

    public static WebApplicationBuilder AddHealthConfiguration(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database", tags: [ReadyTag]);

        return builder;
    }

    public static void MapHealthEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health", new() { Predicate = _ => false })
            .AllowAnonymous()
            .DisableRateLimiting();

        app.MapHealthChecks("/health/ready", new() { Predicate = check => check.Tags.Contains(ReadyTag) })
            .AllowAnonymous()
            .DisableRateLimiting();
    }
}

internal sealed class DatabaseHealthCheck(AppDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Baza de date nu răspunde.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Baza de date nu răspunde.", exception);
        }
    }
}
