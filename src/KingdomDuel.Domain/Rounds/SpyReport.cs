using KingdomDuel.Domain.Enums;

namespace KingdomDuel.Domain.Rounds;

public sealed record SpyOrders(FeastTier? Feast, int Defenders, bool Recruited, bool TouchedSpies);

public sealed record SpyReport(
    int Round,
    SpyTier Tier,
    bool CaravanSent,
    int CaravanVillagers,
    int Fortification,
    int Villagers,
    int Troops,
    int Morale,
    SpyOrders? Orders);
