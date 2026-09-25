using KingdomDuel.Domain.Enums;

namespace KingdomDuel.Domain.Rounds;

public static class Spying
{
    public static int Blur(int truth, double accuracy, Random random)
    {
        if (truth == 0)
        {
            return 0;
        }

        var band = 1 - accuracy;
        var drift = (random.NextDouble() * 2 - 1) * band;

        return Math.Max(0, (int)Math.Round(truth * (1 + drift)));
    }

    public static SpyReport? ReportOn(
        int round,
        SpyRing? ring,
        SideOutcome watched,
        OrderPlan? watchedOrder,
        Random random)
    {
        if (ring is not { } spies)
        {
            return null;
        }

        var accuracy = GameConstants.SpyAccuracy(spies.Tier);
        var kingdom = watched.Kingdom;

        return new SpyReport(
            round,
            spies.Tier,
            watched.Caravan is not null,
            Blur(watched.Caravan?.Troops ?? 0, accuracy, random),
            kingdom.Fortification,
            Blur(kingdom.Villagers, accuracy, random),
            Blur(kingdom.Troops, accuracy, random),
            Blur(kingdom.Morale, accuracy, random),
            GameConstants.SpiesReadOrders(spies.Tier) && watchedOrder is { } order
                ? new SpyOrders(
                    order.Feast,
                    order[ActionType.Defend],
                    order.Has(PurchaseType.Recruit),
                    watched.Spies is not null)
                : null);
    }
}
