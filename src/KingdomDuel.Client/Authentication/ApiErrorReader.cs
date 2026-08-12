using System.Text.Json;
using KingdomDuel.Client.ApiClients;

namespace KingdomDuel.Client.Authentication;

public static class ApiErrorReader
{
    public static string Describe(ApiException exception, string fallback)
    {
        var messages = ReadMessages(exception.Response);

        if (messages.Count > 0)
        {
            return string.Join(" ", messages);
        }

        return ReadMessage(exception.Response) ?? fallback;
    }

    private static string? ReadMessage(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(body);

            return document.RootElement.TryGetProperty("message", out var message)
                   && message.ValueKind == JsonValueKind.String
                ? message.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
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
