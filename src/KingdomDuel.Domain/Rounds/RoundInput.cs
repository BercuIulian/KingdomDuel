using System.Text.Json.Serialization;
using KingdomDuel.Domain.Enums;

namespace KingdomDuel.Domain.Rounds;

public sealed record SideInput(
    KingdomState Kingdom,
    OrderPlan Order,
    Caravan? Caravan = null);

public sealed record RoundInput(
    int Round,
    int TotalRounds,
    SideInput Player1,
    SideInput Player2);

public sealed record SideOutcome(KingdomState Kingdom, Caravan? Caravan = null, int Recruits = 0)
{
    [JsonIgnore]
    public int TotalPower => Kingdom.TotalPower + (Caravan?.Troops ?? 0);
}

public sealed record RoundOutcome(
    SideOutcome Player1,
    SideOutcome Player2,
    IReadOnlyList<RoundEvent> Events,
    GameOverReason GameOverReason,
    [property: JsonIgnore] BattleReplay? Battle = null);
