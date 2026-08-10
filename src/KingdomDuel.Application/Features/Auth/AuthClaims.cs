using System.Security.Claims;

namespace KingdomDuel.Application.Features.Auth;

internal static class AuthClaims
{
    public const string UserId = "sub";
    public const string UserName = "name";
    public const string TokenVersion = "ver";
    public const string Expiry = "exp";

    public static Guid GetUserId(this ClaimsPrincipal principal)
        => principal.FindUserId()
           ?? throw new InvalidOperationException(
               $"Claim-ul '{UserId}' lipsește. Endpoint-ul rulează fără autentificare?");

    public static Guid? FindUserId(this ClaimsPrincipal principal)
        => Guid.TryParse(principal.FindFirstValue(UserId), out var id) ? id : null;

    public static string? FindUserName(this ClaimsPrincipal principal)
        => principal.FindFirstValue(UserName);

    public static int? FindTokenVersion(this ClaimsPrincipal principal)
        => int.TryParse(principal.FindFirstValue(TokenVersion), out var version) ? version : null;

    public static DateTimeOffset? FindExpiry(this ClaimsPrincipal principal)
        => long.TryParse(principal.FindFirstValue(Expiry), out var seconds)
            ? DateTimeOffset.FromUnixTimeSeconds(seconds)
            : null;
}
