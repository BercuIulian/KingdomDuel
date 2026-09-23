using KingdomDuel.Domain.Rounds;

namespace KingdomDuel.Shared.Dtos.Matches;

public sealed class BattleDto
{
    public int AttackerSide { get; set; }
    public BattlePhase Phase { get; set; }
    public int Player1Start { get; set; }
    public int Player2Start { get; set; }
    public int Player1Left { get; set; }
    public int Player2Left { get; set; }
    public double Elapsed { get; set; }
    public double Duration { get; set; }
    public double? BreachAt { get; set; }
    public int? KeepDefenders { get; set; }
    public bool Over { get; set; }
    public int? RetreatBySide { get; set; }
    public double? RetreatAt { get; set; }
    public bool CanRetreat { get; set; }
    public List<BattleFallDto> Falls { get; set; } = [];
}
