namespace KingdomDuel.Shared.Requests.Matches;

public sealed class RallyRequest
{
    public string Code { get; set; } = string.Empty;
    public int Villagers { get; set; }
}
