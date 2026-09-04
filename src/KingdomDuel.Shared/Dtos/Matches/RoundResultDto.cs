using KingdomDuel.Domain.Enums;

namespace KingdomDuel.Shared.Dtos.Matches;

public sealed class RoundResultDto
{
    public int RoundNumber { get; set; }
    public GameEventType Event { get; set; }
    public ActionType Player1Action { get; set; }
    public ActionType Player2Action { get; set; }
    public int Player1AttackTroops { get; set; }
    public int Player2AttackTroops { get; set; }
    public List<string> Log { get; set; } = new();
}
