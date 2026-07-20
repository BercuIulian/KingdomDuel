namespace KingdomDuel.Client.ApiClients;

/// <summary>
/// Mirrors the default FastEndpoints error payload. The NSwag-generated
/// <see cref="MatchesApiClient"/> references this type for 400 responses but the
/// generator did not emit its definition, so it lives here as a hand-written POCO.
/// </summary>
public sealed class ErrorResponse
{
    public int StatusCode { get; set; }

    public string Message { get; set; } = string.Empty;

    public Dictionary<string, List<string>> Errors { get; set; } = new();
}
