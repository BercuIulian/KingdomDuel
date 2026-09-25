using KingdomDuel.Domain.Contracts;
using KingdomDuel.Domain.Enums;
using KingdomDuel.Domain.Rounds;

namespace KingdomDuel.Domain.Entities.Matches;

public sealed class Match : Entity
{
    public required string Code { get; init; }
    public MatchStatus Status { get; set; } = MatchStatus.WaitingForPlayer;
    public int Round { get; set; } = 1;
    public GameOverReason GameOverReason { get; set; } = GameOverReason.None;

    public int TotalRounds { get; set; } = GameConstants.DefaultTotalRounds;
    public int StartingGold { get; set; } = GameConstants.StartingGold;
    public int StartingTroops { get; set; } = GameConstants.StartingSoldiers;

    public int StartingVillagers { get; set; } = GameConstants.StartingVillagers;
    public int StartingMorale { get; set; } = GameConstants.StartingResource;
    public int RoundSeconds { get; set; } = GameConstants.DefaultRoundSeconds;
    public DateTimeOffset? RoundDeadline { get; set; }

    public LiveBattle? LiveBattle { get; set; }

    public MarchChoice? MarchChoice { get; set; }

    public PlayerSlot? Player1 { get; set; }
    public PlayerSlot? Player2 { get; set; }

    public List<RoundResult> History { get; private set; } = new();

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
