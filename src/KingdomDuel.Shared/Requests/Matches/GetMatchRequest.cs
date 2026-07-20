namespace KingdomDuel.Shared.Requests.Matches;

public sealed class GetMatchRequest
{
    /// <summary>Bound from the route.</summary>
    public string Code { get; set; } = string.Empty;
}
