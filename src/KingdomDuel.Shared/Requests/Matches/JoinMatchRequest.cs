namespace KingdomDuel.Shared.Requests.Matches;

public sealed class JoinMatchRequest
{
    /// <summary>Bound from the route.</summary>
    public string Code { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;
}
