using System.Text.Json.Serialization;

namespace KingdomDuel.Domain.Rounds;

public sealed record RoundSide(
    OrderPlan Order,
    KingdomState After,
    Caravan? Caravan = null,
    SpyRing? Spies = null)
{
    [JsonIgnore]
    public int TotalPower => SideOutcome.PowerOf(After, Caravan, Spies);
}
