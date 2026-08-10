using FastEndpoints.Security;
using KingdomDuel.Domain.Entities.Users;
using Microsoft.Extensions.Options;

namespace KingdomDuel.Application.Features.Auth.Services;

internal sealed class TokenIssuer(IOptions<JwtOptions> options)
{
    private readonly JwtOptions options = options.Value;

    public AuthToken Issue(User user)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddDays(options.LifetimeDays);

        var token = JwtBearer.CreateToken(o =>
        {
            o.SigningKey = options.SigningKey;
            o.Issuer = options.Issuer;
            o.Audience = options.Audience;
            o.ExpireAt = expiresAt.UtcDateTime;
            o.User[AuthClaims.UserId] = user.Id.ToString();
            o.User[AuthClaims.UserName] = user.UserName;
        });

        return new AuthToken(token, expiresAt);
    }
}
