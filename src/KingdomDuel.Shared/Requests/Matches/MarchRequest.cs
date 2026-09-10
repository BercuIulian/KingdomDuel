namespace KingdomDuel.Shared.Requests.Matches;

public sealed class MarchRequest
{
    public string Code { get; set; } = string.Empty;
    public bool Advance { get; set; }
}
