using System.Collections.Immutable;
using KingdomDuel.Domain.Enums;

namespace KingdomDuel.Domain.Rounds;

public sealed record OrderPlan
{
    public static readonly OrderPlan Empty = new(ImmutableDictionary<ActionType, int>.Empty);

    private readonly ImmutableDictionary<ActionType, int> troops;
    private readonly ImmutableHashSet<PurchaseType> purchases;

    public OrderPlan(
        IReadOnlyDictionary<ActionType, int> troops,
        FeastTier? feast = null,
        IEnumerable<PurchaseType>? purchases = null,
        int hired = 0,
        int armed = 0,
        SpyTier? spies = null,
        bool recallSpies = false)
    {
        this.troops = troops
            .Where(pair => pair.Value > 0)
            .ToImmutableDictionary(pair => pair.Key, pair => pair.Value);

        this.purchases = purchases?.ToImmutableHashSet() ?? ImmutableHashSet<PurchaseType>.Empty;

        Feast = feast;
        Hired = Math.Max(0, hired);
        Armed = Math.Max(0, armed);
        Spies = recallSpies ? null : spies;
        RecallSpies = recallSpies;

        Committed = this[ActionType.Attack] + this[ActionType.Defend];
        Sent = this[ActionType.Trade];
    }

    public IReadOnlyDictionary<ActionType, int> Troops => troops;

    public IReadOnlyCollection<PurchaseType> Purchases => purchases;

    public FeastTier? Feast { get; }

    public SpyTier? Spies { get; }

    public bool RecallSpies { get; }

    public int Hired { get; }

    public int Armed { get; }

    public int Committed { get; }

    public int Sent { get; }

    public int GoldCost => Cost(Feast, purchases, Hired, Armed);

    public int this[ActionType direction] => troops.GetValueOrDefault(direction);

    public bool Has(PurchaseType purchase) => purchases.Contains(purchase);

    public int ReserveIn(KingdomState state) => Math.Max(0, state.Troops - Committed);

    public bool FitsIn(KingdomState state) =>
        Committed <= state.Troops && Sent <= state.Villagers && GoldCost <= state.Gold;

    public OrderPlan ClampTo(KingdomState state)
    {
        var kept = purchases;
        var feast = Feast;
        var hired = Hired;
        var armed = Math.Min(Armed, state.Villagers);

        while (Cost(feast, kept, hired, armed) > state.Gold && hired > 0)
        {
            hired -= 1;
        }

        while (Cost(feast, kept, hired, armed) > state.Gold && armed > 0)
        {
            armed -= 1;
        }

        while (Cost(feast, kept, hired, armed) > state.Gold && kept.Count > 0)
        {
            kept = kept.Remove(kept.Max());
        }

        if (Cost(feast, kept, hired, armed) > state.Gold)
        {
            feast = null;
        }

        var scaled = Fit(troops, ActionType.Attack, ActionType.Defend, Committed, state.Troops);
        scaled = Fit(scaled, ActionType.Trade, ActionType.Trade, Sent, state.Villagers);

        var spies = Spies;

        while (spies is { } tier
            && (GameConstants.SpyVillagers(tier) > state.Villagers
                || GameConstants.SpyUpkeep(tier) + Cost(feast, kept, hired, armed) > state.Gold))
        {
            spies = tier == SpyTier.Lone ? null : tier - 1;
        }

        return new OrderPlan(scaled, feast, kept, hired, armed, spies, RecallSpies);
    }

    public static OrderPlan Of(ActionType direction, int count) =>
        new(new Dictionary<ActionType, int> { [direction] = count });

    private static IReadOnlyDictionary<ActionType, int> Fit(
        IReadOnlyDictionary<ActionType, int> parts,
        ActionType first,
        ActionType second,
        int asked,
        int available)
    {
        if (asked <= available)
        {
            return parts;
        }

        var cut = parts.ToDictionary(pair => pair.Key, pair => pair.Value);

        if (available <= 0 || asked <= 0)
        {
            cut[first] = 0;
            cut[second] = 0;

            return cut;
        }

        cut[first] = parts.GetValueOrDefault(first) * available / asked;

        if (second != first)
        {
            cut[second] = parts.GetValueOrDefault(second) * available / asked;
        }

        return cut;
    }

    private static int Cost(
        FeastTier? feast,
        ImmutableHashSet<PurchaseType> purchases,
        int hired,
        int armed) =>
        (feast is { } tier ? GameConstants.FeastCost(tier) : 0)
        + purchases.Sum(GameConstants.PurchaseCost)
        + hired * GameConstants.VillagerBundleCost
        + armed * GameConstants.ArmCost;
}
