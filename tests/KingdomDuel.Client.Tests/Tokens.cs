using System.Text;
using System.Text.Json;

namespace KingdomDuel.Client.Tests;

internal static class Tokens
{
    public static string With(object payload)
    {
        var header = Encode("""{"alg":"HS256","typ":"JWT"}""");
        var body = Encode(JsonSerializer.Serialize(payload));

        return $"{header}.{body}.semnatura";
    }

    public static string ValidFor(TimeSpan lifetime, string name = "rege") =>
        With(new Dictionary<string, object>
        {
            ["sub"] = "11111111-1111-1111-1111-111111111111",
            ["name"] = name,
            ["exp"] = DateTimeOffset.UtcNow.Add(lifetime).ToUnixTimeSeconds(),
        });

    public static string Fresh(string name = "rege") => ValidFor(TimeSpan.FromDays(7), name);

    public static string Expired() => ValidFor(TimeSpan.FromMinutes(-1));

    private static string Encode(string json) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(json)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
