using KingdomDuel.Domain.Contracts;
using KingdomDuel.Domain.Enums;

namespace KingdomDuel.Domain.Entities.Matches;

public sealed class RoundResult : Entity
{
    public required int RoundNumber { get; init; }
    public required GameEventType Event { get; init; }
    public required ActionType Player1Action { get; init; }
    public required ActionType Player2Action { get; init; }
    public List<string> Log { get; init; } = new();
    public GameOverReason GameOverReason { get; init; } = GameOverReason.None;
}
