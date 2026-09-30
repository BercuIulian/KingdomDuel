using KingdomDuel.Application.Features.Auth.Services;
using KingdomDuel.Domain.Entities.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace KingdomDuel.Application.Features.Auth;

public static class AuthServiceConfiguration
{
    public static IServiceCollection AddAuthServices(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<TokenIssuer>();
        services.AddScoped<TokenRenewer>();
        services.AddScoped<TokenRevoker>();
        services.AddScoped<TokenVersionValidator>();
        services.AddScoped<UserRegistrar>();
        services.AddScoped<UserAuthenticator>();
        services.AddScoped<UserProfileEditor>();

        return services;
    }
}
