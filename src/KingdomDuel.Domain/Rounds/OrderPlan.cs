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
        IEnumerable<PurchaseType>? purchases = null)
    {
        this.troops = troops
            .Where(pair => pair.Value > 0)
            .ToImmutableDictionary(pair => pair.Key, pair => pair.Value);

        this.purchases = purchases?.ToImmutableHashSet() ?? ImmutableHashSet<PurchaseType>.Empty;

        Committed = this.troops.Values.Sum();
        Feast = feast;
    }

    public IReadOnlyDictionary<ActionType, int> Troops => troops;

    public IReadOnlyCollection<PurchaseType> Purchases => purchases;

    public FeastTier? Feast { get; }

    public int Committed { get; }

    public int GoldCost =>
        (Feast is { } tier ? GameConstants.FeastCost(tier) : 0)
        + purchases.Sum(GameConstants.PurchaseCost);

    public int this[ActionType direction] => troops.GetValueOrDefault(direction);

    public bool Has(PurchaseType purchase) => purchases.Contains(purchase);

    public int ReserveIn(KingdomState state) => Math.Max(0, state.Troops - Committed);

    public bool FitsIn(KingdomState state) => Committed <= state.Troops && GoldCost <= state.Gold;

    public OrderPlan ClampTo(KingdomState state)
    {
        var kept = purchases;
        var feast = Feast;

        while (Cost(feast, kept) > state.Gold && kept.Count > 0)
        {
            kept = kept.Remove(kept.Max());
        }

        if (Cost(feast, kept) > state.Gold)
        {
            feast = null;
        }

        if (Committed <= state.Troops)
        {
            return feast == Feast && kept.Count == purchases.Count
                ? this
                : new OrderPlan(troops, feast, kept);
        }

        if (state.Troops <= 0 || Committed <= 0)
        {
            return new OrderPlan(ImmutableDictionary<ActionType, int>.Empty, feast, kept);
        }

        var scaled = troops.ToDictionary(
            pair => pair.Key,
            pair => pair.Value * state.Troops / Committed);

        return new OrderPlan(scaled, feast, kept);
    }

    public static OrderPlan Of(ActionType direction, int count) =>
        new(new Dictionary<ActionType, int> { [direction] = count });

    private static int Cost(FeastTier? feast, ImmutableHashSet<PurchaseType> purchases) =>
        (feast is { } tier ? GameConstants.FeastCost(tier) : 0)
        + purchases.Sum(GameConstants.PurchaseCost);
}
