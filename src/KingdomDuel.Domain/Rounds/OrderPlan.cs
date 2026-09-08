using System.Collections.Immutable;
using KingdomDuel.Domain.Enums;

namespace KingdomDuel.Domain.Rounds;

public sealed record OrderPlan
{
    public static readonly OrderPlan Empty = new(ImmutableDictionary<ActionType, int>.Empty);

    private readonly ImmutableDictionary<ActionType, int> troops;

    public OrderPlan(IReadOnlyDictionary<ActionType, int> troops, FeastTier? feast = null)
    {
        this.troops = troops
            .Where(pair => pair.Value > 0)
            .ToImmutableDictionary(pair => pair.Key, pair => pair.Value);

        Committed = this.troops.Values.Sum();
        Feast = feast;
    }

    public IReadOnlyDictionary<ActionType, int> Troops => troops;

    public FeastTier? Feast { get; }

    public int Committed { get; }

    public int GoldCost => Feast is { } tier ? GameConstants.FeastCost(tier) : 0;

    public int this[ActionType direction] => troops.GetValueOrDefault(direction);

    public int ReserveIn(KingdomState state) => Math.Max(0, state.Troops - Committed);

    public bool FitsIn(KingdomState state) => Committed <= state.Troops && GoldCost <= state.Gold;

    public OrderPlan ClampTo(KingdomState state)
    {
        var feast = GoldCost <= state.Gold ? Feast : null;

        if (Committed <= state.Troops)
        {
            return feast == Feast ? this : new OrderPlan(troops, feast);
        }

        if (state.Troops <= 0 || Committed <= 0)
        {
            return new OrderPlan(ImmutableDictionary<ActionType, int>.Empty, feast);
        }

        var scaled = troops.ToDictionary(
            pair => pair.Key,
            pair => pair.Value * state.Troops / Committed);

        return new OrderPlan(scaled, feast);
    }

    public static OrderPlan Of(ActionType direction, int count) =>
        new(new Dictionary<ActionType, int> { [direction] = count });
}
