using KingdomDuel.Domain.Rounds;

namespace KingdomDuel.Shared.Dtos.Matches;

public sealed class BattleFallDto
{
    public double At { get; set; }
    public int Side { get; set; }
    public BattlePhase Phase { get; set; }
}
