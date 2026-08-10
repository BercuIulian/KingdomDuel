using System.Text.Json;
using KingdomDuel.Client.ApiClients;

namespace KingdomDuel.Client.Authentication;

public static class ApiErrorReader
{
    public static string Describe(ApiException exception, string fallback)
    {
        var messages = ReadMessages(exception.Response);

        return messages.Count > 0 ? string.Join(" ", messages) : fallback;
    }

    private static List<string> ReadMessages(string? body)
    {
        var messages = new List<string>();

        if (string.IsNullOrWhiteSpace(body))
        {
            return messages;
        }

        try
        {
            using var document = JsonDocument.Parse(body);

            if (!document.RootElement.TryGetProperty("errors", out var errors)
                || errors.ValueKind != JsonValueKind.Object)
            {
                return messages;
            }

            foreach (var field in errors.EnumerateObject())
            {
                if (field.Value.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                messages.AddRange(field.Value
                    .EnumerateArray()
                    .Where(m => m.ValueKind == JsonValueKind.String)
                    .Select(m => m.GetString()!));
            }
        }
        catch (JsonException)
        {
            return messages;
        }

        return messages;
    }
}
