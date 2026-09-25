using System.Text.Json.Serialization;

namespace KingdomDuel.Domain.Rounds;

public readonly record struct KingdomState(
    int Gold,
    int Troops,
    int Morale,
    int Villagers = 0,
    int Fortification = 0,
    int Catapults = 0,
    int RuinedFrom = 0)
{
    public static KingdomState Start(int gold, int troops, int morale, int villagers = 0) =>
        new(gold, troops, morale, villagers);

    [JsonIgnore]
    public bool WallIsDown => Fortification <= GameConstants.WallDownLevel;

    [JsonIgnore]
    public int TotalPower => Gold + Troops + Morale + Villagers;

    public KingdomState With(int goldDelta = 0, int troopsDelta = 0, int moraleDelta = 0, int villagersDelta = 0) => this with
    {
        Gold = Math.Max(0, Gold + goldDelta),
        Troops = Math.Max(0, Troops + troopsDelta),
        Morale = Math.Max(0, Morale + moraleDelta),
        Villagers = Math.Max(0, Villagers + villagersDelta),
    };
}
