using System.Text.Json.Serialization;

namespace KingdomDuel.Domain.Rounds;

public readonly record struct KingdomState(
    int Gold,
    int Troops,
    int Morale,
    int Fortification = 0,
    int Catapults = 0)
{
    public static KingdomState Start(int gold, int troops, int morale) => new(gold, troops, morale);

    [JsonIgnore]
    public int TotalPower => Gold + Troops + Morale;

    public KingdomState With(int goldDelta = 0, int troopsDelta = 0, int moraleDelta = 0) => this with
    {
        Gold = Math.Max(0, Gold + goldDelta),
        Troops = Math.Max(0, Troops + troopsDelta),
        Morale = Math.Max(0, Morale + moraleDelta),
    };
}
