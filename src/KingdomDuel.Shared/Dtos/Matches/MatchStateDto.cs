using KingdomDuel.Domain.Enums;

namespace KingdomDuel.Shared.Dtos.Matches;

public sealed class MatchStateDto
{
    public required string Code { get; set; }
    public int YourSide { get; set; }
    public bool IsHost { get; set; }
    public MatchStatus Status { get; set; }
    public int Round { get; set; }
    public GameEventType CurrentEvent { get; set; }
    public GameOverReason GameOverReason { get; set; }
    public int TotalRounds { get; set; }
    public int StartingGold { get; set; }
    public int StartingTroops { get; set; }
    public int StartingMorale { get; set; }
    public int RoundSeconds { get; set; }
    public int? SecondsRemaining { get; set; }
    public string? Player1Name { get; set; }
    public string? Player2Name { get; set; }
    public bool Player1Ready { get; set; }
    public bool Player2Ready { get; set; }
    public int Player1MissedRounds { get; set; }
    public int Player2MissedRounds { get; set; }
    public KingdomDto? Player1Kingdom { get; set; }
    public KingdomDto? Player2Kingdom { get; set; }
    public bool WaitingOnPlayer1Action { get; set; }
    public bool WaitingOnPlayer2Action { get; set; }
    public List<RoundResultDto> History { get; set; } = new();
}
