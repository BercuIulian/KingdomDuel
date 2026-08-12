using System.Security.Claims;
using Microsoft.Extensions.Options;

namespace KingdomDuel.Application.Features.Auth.Services;

internal sealed class TokenRenewer(TokenIssuer tokenIssuer, IOptions<JwtOptions> options)
{
    private readonly JwtOptions options = options.Value;

    public AuthToken? RenewIfNearExpiry(ClaimsPrincipal principal)
    {
        var expiresAt = principal.FindExpiry();
        var userId = principal.FindUserId();
        var userName = principal.FindUserName();
        var version = principal.FindTokenVersion();

        if (expiresAt is null || userId is null || userName is null || version is null)
        {
            return null;
        }

        var remaining = expiresAt.Value - DateTimeOffset.UtcNow;

        return remaining > TimeSpan.FromDays(options.RenewWhenRemainingDays)
            ? null
            : tokenIssuer.Issue(userId.Value, userName, version.Value);
    }
}
