namespace KingdomDuel.Shared.Requests.Matches;

public sealed class LeaveMatchRequest
{
    public string Code { get; set; } = string.Empty;

    public string PlayerId { get; set; } = string.Empty;
}
