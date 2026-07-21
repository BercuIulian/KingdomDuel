using KingdomDuel.Domain.Contracts;
using KingdomDuel.Domain.Enums;

namespace KingdomDuel.Domain.Entities.Matches;

public sealed class Match : Entity
{
    public required string Code { get; init; }
    public MatchStatus Status { get; set; } = MatchStatus.WaitingForPlayer;
    public int Round { get; set; } = 1;
    public GameEventType CurrentEvent { get; set; } = GameEventType.None;
    public GameOverReason GameOverReason { get; set; } = GameOverReason.None;

    public int TotalRounds { get; set; } = 8;
    public int StartingGold { get; set; } = 50;
    public int StartingTroops { get; set; } = 50;
    public int StartingMorale { get; set; } = 50;

    public PlayerSlot? Player1 { get; set; }
    public PlayerSlot? Player2 { get; set; }

     public Dictionary<string, ActionType> PendingActions { get; private set; } = new();

    public List<RoundResult> History { get; private set; } = new();

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
