using KingdomDuel.Domain.Enums;

namespace KingdomDuel.Shared.Dtos.Matches;

public sealed class MatchHistoryEntryDto
{
    public required string OpponentName { get; set; }
    public MatchOutcome Outcome { get; set; }
    public GameOverReason Reason { get; set; }
    public int RoundsPlayed { get; set; }
    public int TotalRounds { get; set; }
    public int YourPower { get; set; }
    public int OpponentPower { get; set; }
    public DateTimeOffset StartedAt { get; set; }
}
