using KingdomDuel.Domain.Contracts;
using KingdomDuel.Domain.Enums;
using KingdomDuel.Domain.Rounds;

namespace KingdomDuel.Domain.Entities.Matches;

public sealed class RoundResult : Entity
{
    public required int RoundNumber { get; init; }
    public required RoundSide Player1 { get; init; }
    public required RoundSide Player2 { get; init; }

    public List<RoundEvent> Events { get; init; } = new();

    public GameOverReason GameOverReason { get; init; } = GameOverReason.None;
}
