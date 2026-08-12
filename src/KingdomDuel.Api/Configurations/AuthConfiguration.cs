using System.Text;
using FastEndpoints.Security;
using KingdomDuel.Application.Features.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace KingdomDuel.Api.Configurations;

public static class AuthConfiguration
{
    private const int MinimumSigningKeyBytes = 32;

    public static WebApplicationBuilder AddAuthConfiguration(this WebApplicationBuilder builder)
    {
        var section = builder.Configuration.GetSection(JwtOptions.SectionName);
        var jwt = section.Get<JwtOptions>() ?? new JwtOptions();

        if (Encoding.UTF8.GetByteCount(jwt.SigningKey) < MinimumSigningKeyBytes)
        {
            throw new InvalidOperationException(
                $"'{JwtOptions.SectionName}:SigningKey' lipsește sau are sub {MinimumSigningKeyBytes} de octeți. " +
                "În producție se setează prin variabila de mediu Jwt__SigningKey.");
        }

        builder.Services.Configure<JwtOptions>(section);
        builder.Services.AddAuthServices();

        builder.Services.AddAuthenticationJwtBearer(
            signing => signing.SigningKey = jwt.SigningKey,
            bearer =>
            {
                bearer.MapInboundClaims = false;

                var validation = bearer.TokenValidationParameters;
                validation.ValidateIssuer = true;
                validation.ValidIssuer = jwt.Issuer;
                validation.ValidateAudience = true;
                validation.ValidAudience = jwt.Audience;
                validation.ClockSkew = TimeSpan.FromMinutes(1);

                var events = bearer.Events ??= new JwtBearerEvents();
                var inner = events.OnTokenValidated;

                events.OnTokenValidated = async context =>
                {
                    if (inner is not null)
                    {
                        await inner(context);
                    }

                    await context.RejectStaleTokenAsync();
                };
            });

        builder.Services.AddAuthorization();

        return builder;
    }

    public static void UseAuthConfiguration(this WebApplication app)
    {
        app.UseAuthentication();
        app.UseTokenRenewal();
        app.UseAuthorization();
    }
}
