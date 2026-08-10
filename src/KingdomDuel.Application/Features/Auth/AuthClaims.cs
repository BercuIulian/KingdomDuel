using System.Security.Claims;

namespace KingdomDuel.Application.Features.Auth;

internal static class AuthClaims
{
    public const string UserId = "sub";
    public const string UserName = "name";

    public static Guid GetUserId(this ClaimsPrincipal principal)
        => Guid.TryParse(principal.FindFirstValue(UserId), out var id)
            ? id
            : throw new InvalidOperationException(
                $"Claim-ul '{UserId}' lipsește. Endpoint-ul rulează fără autentificare?");
}
