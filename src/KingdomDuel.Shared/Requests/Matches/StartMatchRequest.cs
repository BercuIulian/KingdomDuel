namespace KingdomDuel.Shared.Requests.Matches;

public sealed class StartMatchRequest
{
    public string Code { get; set; } = string.Empty;
    public string PlayerId { get; set; } = string.Empty;
}
