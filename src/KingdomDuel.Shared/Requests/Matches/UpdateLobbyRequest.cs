namespace KingdomDuel.Shared.Requests.Matches;

public sealed class UpdateLobbyRequest
{
    public string Code { get; set; } = string.Empty;
    public string PlayerId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsReady { get; set; }
    public int TotalRounds { get; set; }
    public int StartingGold { get; set; }
    public int StartingTroops { get; set; }
    public int StartingMorale { get; set; }
}
