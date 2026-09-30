using System.Text.Json.Serialization;
using KingdomDuel.Domain.Enums;

namespace KingdomDuel.Domain.Rounds;

public sealed record SideInput(
    KingdomState Kingdom,
    OrderPlan Order,
    Caravan? Caravan = null,
    SpyRing? Spies = null);

public sealed record RoundInput(
    int Round,
    int TotalRounds,
    SideInput Player1,
    SideInput Player2);

public sealed record SideOutcome(
    KingdomState Kingdom,
    Caravan? Caravan = null,
    int Recruits = 0,
    SpyRing? Spies = null,
    int Hired = 0)
{
    [JsonIgnore]
    public int TotalPower => PowerOf(Kingdom, Caravan, Spies);

    public static int PowerOf(KingdomState kingdom, Caravan? caravan, SpyRing? spies) =>
        kingdom.TotalPower + (caravan?.Troops ?? 0) + (spies?.Villagers ?? 0);
}

public sealed record RoundOutcome(
    SideOutcome Player1,
    SideOutcome Player2,
    IReadOnlyList<RoundEvent> Events,
    GameOverReason GameOverReason,
    [property: JsonIgnore] BattleReplay? Battle = null);
