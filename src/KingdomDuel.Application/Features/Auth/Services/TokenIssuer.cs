using FastEndpoints.Security;
using KingdomDuel.Domain.Entities.Users;
using Microsoft.Extensions.Options;

namespace KingdomDuel.Application.Features.Auth.Services;

internal sealed class TokenIssuer(IOptions<JwtOptions> options, TimeProvider time)
{
    private readonly JwtOptions options = options.Value;

    public AuthToken Issue(User user) => Issue(user.Id, user.UserName, user.TokenVersion);

    public AuthToken Issue(Guid userId, string userName, int tokenVersion)
    {
        var expiresAt = time.GetUtcNow().AddDays(options.LifetimeDays);

        var token = JwtBearer.CreateToken(o =>
        {
            o.SigningKey = options.SigningKey;
            o.Issuer = options.Issuer;
            o.Audience = options.Audience;
            o.ExpireAt = expiresAt.UtcDateTime;
            o.User[AuthClaims.UserId] = userId.ToString();
            o.User[AuthClaims.UserName] = userName;
            o.User[AuthClaims.TokenVersion] = tokenVersion.ToString();
        });

        return new AuthToken(token, expiresAt);
    }
}
