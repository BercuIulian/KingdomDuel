using KingdomDuel.Domain.Enums;

namespace KingdomDuel.Domain.Rounds;

public sealed record SpyRing(SpyTier Tier)
{
    public int Villagers => GameConstants.SpyVillagers(Tier);

    public int Upkeep => GameConstants.SpyUpkeep(Tier);
}
