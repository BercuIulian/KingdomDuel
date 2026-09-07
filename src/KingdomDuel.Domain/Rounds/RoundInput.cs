using KingdomDuel.Domain.Enums;

namespace KingdomDuel.Domain.Rounds;

public sealed record SideInput(
    KingdomState Kingdom,
    OrderPlan Order,
    Caravan? Caravan = null);

public sealed record RoundInput(
    int Round,
    int TotalRounds,
    GameEventType Event,
    SideInput Player1,
    SideInput Player2);

public sealed record SideOutcome(KingdomState Kingdom, Caravan? Caravan = null)
{
    public int TotalPower => Kingdom.TotalPower + (Caravan?.Troops ?? 0);
}

public sealed record RoundOutcome(
    SideOutcome Player1,
    SideOutcome Player2,
    IReadOnlyList<RoundEvent> Events,
    GameOverReason GameOverReason);
