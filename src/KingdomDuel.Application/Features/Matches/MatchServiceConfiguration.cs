using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace KingdomDuel.Application.Features.Matches;

public static class MatchServiceConfiguration
{
    public static IServiceCollection AddMatchServices(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton(Random.Shared);
        services.AddScoped<RoundResolver>();

        return services;
    }
}
