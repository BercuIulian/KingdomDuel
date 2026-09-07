using KingdomDuel.Domain.Enums;

namespace KingdomDuel.Domain.Rounds;

public sealed record SideInput(KingdomState Kingdom, OrderPlan Order);

public sealed record RoundInput(
    int Round,
    int TotalRounds,
    GameEventType Event,
    SideInput Player1,
    SideInput Player2);

public sealed record RoundOutcome(
    KingdomState Player1,
    KingdomState Player2,
    IReadOnlyList<RoundEvent> Events,
    GameOverReason GameOverReason);
