using System.Text.Json.Serialization;

namespace KingdomDuel.Domain.Rounds;

public readonly record struct KingdomState(int Gold, int Troops, int Morale)
{
    public static KingdomState Start(int gold, int troops, int morale) => new(gold, troops, morale);

    [JsonIgnore]
    public int TotalPower => Gold + Troops + Morale;

    [JsonIgnore]
    public bool HasCollapsed => Troops <= 0 || Morale <= 0;

    public KingdomState With(int goldDelta = 0, int troopsDelta = 0, int moraleDelta = 0) => new(
        Math.Max(0, Gold + goldDelta),
        Math.Max(0, Troops + troopsDelta),
        Math.Max(0, Morale + moraleDelta));
}
