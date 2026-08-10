using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace KingdomDuel.Client.Authentication;

public static class JwtPayload
{
    public const string ExpiryClaim = "exp";
    public const string NameClaim = "name";

    public static IReadOnlyList<Claim> ReadClaims(string token)
    {
        var payload = ReadPayload(token);

        if (payload is null)
        {
            return [];
        }

        var claims = new List<Claim>();

        foreach (var property in payload)
        {
            var value = property.Value.ValueKind == JsonValueKind.String
                ? property.Value.GetString()
                : property.Value.ToString();

            if (value is not null)
            {
                claims.Add(new Claim(property.Key, value));
            }
        }

        return claims;
    }

    public static DateTimeOffset? ReadExpiry(string token)
    {
        var payload = ReadPayload(token);

        if (payload is null
            || !payload.TryGetValue(ExpiryClaim, out var expiry)
            || expiry.ValueKind != JsonValueKind.Number
            || !expiry.TryGetInt64(out var seconds))
        {
            return null;
        }

        return DateTimeOffset.FromUnixTimeSeconds(seconds);
    }

    public static bool IsExpired(string token)
    {
        var expiry = ReadExpiry(token);

        return expiry is null || expiry <= DateTimeOffset.UtcNow;
    }

    private static Dictionary<string, JsonElement>? ReadPayload(string token)
    {
        var parts = token.Split('.');

        if (parts.Length != 3)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(FromBase64Url(parts[1]));
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static byte[] FromBase64Url(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');

        return Convert.FromBase64String(padded.PadRight(padded.Length + (3 - ((padded.Length + 3) % 4)), '='));
    }
}
