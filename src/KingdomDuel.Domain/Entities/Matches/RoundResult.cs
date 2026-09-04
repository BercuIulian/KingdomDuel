using KingdomDuel.Domain.Contracts;
using KingdomDuel.Domain.Enums;

namespace KingdomDuel.Domain.Entities.Matches;

public sealed class RoundResult : Entity
{
    public required int RoundNumber { get; init; }
    public required GameEventType Event { get; init; }
    public required ActionType Player1Action { get; init; }
    public required ActionType Player2Action { get; init; }

    /// <summary>Câte trupe a trimis fiecare în expediție; zero dacă n-a atacat.</summary>
    public int Player1AttackTroops { get; init; }

    public int Player2AttackTroops { get; init; }

    public List<string> Log { get; init; } = new();
    public GameOverReason GameOverReason { get; init; } = GameOverReason.None;
}
