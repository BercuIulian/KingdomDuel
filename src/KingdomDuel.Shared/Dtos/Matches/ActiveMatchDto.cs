namespace KingdomDuel.Shared.Dtos.Matches;

public sealed class ActiveMatchDto
{
    public required string Code { get; set; }
    public int Round { get; set; }
    public int TotalRounds { get; set; }
}
