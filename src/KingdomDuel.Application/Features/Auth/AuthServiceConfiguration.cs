using KingdomDuel.Application.Features.Auth.Services;
using KingdomDuel.Domain.Entities.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace KingdomDuel.Application.Features.Auth;

public static class AuthServiceConfiguration
{
    public static IServiceCollection AddAuthServices(this IServiceCollection services)
    {
        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<TokenIssuer>();
        services.AddScoped<UserRegistrar>();
        services.AddScoped<UserAuthenticator>();

        return services;
    }
}
