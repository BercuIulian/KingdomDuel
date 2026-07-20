using KingdomDuel.Domain.Enums;

namespace KingdomDuel.Shared.Dtos.Matches;

public sealed class MatchStateDto
{
    public required string Code { get; set; }
    public MatchStatus Status { get; set; }
    public int Round { get; set; }
    public GameEventType CurrentEvent { get; set; }
    public GameOverReason GameOverReason { get; set; }
    public string? Player1Name { get; set; }
    public string? Player2Name { get; set; }
    public KingdomDto? Player1Kingdom { get; set; }
    public KingdomDto? Player2Kingdom { get; set; }
    public bool WaitingOnPlayer1Action { get; set; }
    public bool WaitingOnPlayer2Action { get; set; }
    public List<RoundResultDto> History { get; set; } = new();
}
