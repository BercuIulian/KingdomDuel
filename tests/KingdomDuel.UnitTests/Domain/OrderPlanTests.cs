using KingdomDuel.Domain.Enums;
using KingdomDuel.Domain.Rounds;
using static KingdomDuel.UnitTests.Domain.GameSetup;

namespace KingdomDuel.UnitTests.Domain;

public sealed class OrderPlanTests
{
    [Fact]
    public void Empty_or_negative_directions_are_left_out()
    {
        var order = Order(attack: 0, defend: -2, trade: 3);

        order.Troops.Keys.ShouldBe([ActionType.Trade]);
    }

    [Fact]
    public void Soldiers_and_villagers_are_counted_apart()
    {
        var order = Order(attack: 4, defend: 3, trade: 6);

        order.Committed.ShouldBe(7);
        order.Sent.ShouldBe(6);
    }

    [Fact]
    public void Recalling_spies_cancels_sending_them()
    {
        var order = Order(spies: SpyTier.Ring, recallSpies: true);

        order.Spies.ShouldBeNull();
        order.RecallSpies.ShouldBeTrue();
    }

    [Fact]
    public void Negative_hires_and_arms_count_as_none()
    {
        var order = Order(hired: -2, armed: -5);

        order.Hired.ShouldBe(0);
        order.Armed.ShouldBe(0);
    }

    [Fact]
    public void Gold_cost_adds_up_every_line_of_the_order()
    {
        var order = Order(
            feast: FeastTier.Court,
            buy: [PurchaseType.Recruit, PurchaseType.Fortify],
            hired: 2,
            armed: 3,
            hunt: HuntTier.Sweep);

        order.GoldCost.ShouldBe(12 + 12 + 12 + 2 * 8 + 3 + 5);
    }

    [Fact]
    public void Soldiers_searching_for_spies_take_room_in_the_order()
    {
        Order(attack: 8, hunt: HuntTier.Sweep).FitsIn(Kingdom(troops: 10)).ShouldBeFalse();
        Order(attack: 7, hunt: HuntTier.Sweep).FitsIn(Kingdom(troops: 10)).ShouldBeTrue();
    }

    [Fact]
    public void Clamp_cuts_soldiers_proportionally_when_too_many_are_sent()
    {
        var clamped = Order(attack: 10, defend: 10).ClampTo(Kingdom(troops: 10));

        clamped[ActionType.Attack].ShouldBe(5);
        clamped[ActionType.Defend].ShouldBe(5);
    }

    [Fact]
    public void Clamp_cuts_traders_to_the_village()
    {
        Order(trade: 10).ClampTo(Kingdom(villagers: 4))[ActionType.Trade].ShouldBe(4);
    }

    [Fact]
    public void Clamp_gives_up_hired_villagers_first()
    {
        var clamped = Order(feast: FeastTier.Small, buy: [PurchaseType.Recruit], hired: 1, armed: 2)
            .ClampTo(Kingdom(gold: 20));

        clamped.Hired.ShouldBe(0);
        clamped.Armed.ShouldBe(2);
        clamped.Has(PurchaseType.Recruit).ShouldBeTrue();
        clamped.Feast.ShouldBe(FeastTier.Small);
    }

    [Fact]
    public void Clamp_gives_up_the_feast_last()
    {
        var order = Order(feast: FeastTier.Small, buy: [PurchaseType.Fortify], armed: 3);

        var enough = order.ClampTo(Kingdom(gold: 5));
        enough.Armed.ShouldBe(0);
        enough.Purchases.ShouldBeEmpty();
        enough.Feast.ShouldBe(FeastTier.Small);

        order.ClampTo(Kingdom(gold: 4)).Feast.ShouldBeNull();
    }

    [Fact]
    public void Clamp_arms_no_more_villagers_than_the_village_has()
    {
        Order(armed: 20).ClampTo(Kingdom(villagers: 6)).Armed.ShouldBe(6);
    }

    [Fact]
    public void Clamp_steps_spies_down_to_what_the_village_can_spare()
    {
        Order(spies: SpyTier.Ring).ClampTo(Kingdom(villagers: 3)).Spies.ShouldBe(SpyTier.Pair);
        Order(spies: SpyTier.Lone).ClampTo(Kingdom(villagers: 0)).Spies.ShouldBeNull();
    }

    [Fact]
    public void Keeping_or_lowering_the_ring_needs_no_villagers_at_home()
    {
        Order(spies: SpyTier.Pair).ClampTo(Kingdom(villagers: 0), new SpyRing(SpyTier.Pair)).Spies.ShouldBe(SpyTier.Pair);
        Order(spies: SpyTier.Lone).ClampTo(Kingdom(villagers: 0), new SpyRing(SpyTier.Ring)).Spies.ShouldBe(SpyTier.Lone);
    }

    [Fact]
    public void Raising_the_ring_needs_only_the_extra_villagers()
    {
        Order(spies: SpyTier.Ring).ClampTo(Kingdom(villagers: 2), new SpyRing(SpyTier.Pair)).Spies.ShouldBe(SpyTier.Ring);
        Order(spies: SpyTier.Ring).ClampTo(Kingdom(villagers: 1), new SpyRing(SpyTier.Pair)).Spies.ShouldBe(SpyTier.Pair);
    }

    [Fact]
    public void Clamp_steps_spies_down_to_what_the_treasury_can_pay()
    {
        Order(spies: SpyTier.Ring).ClampTo(Kingdom(gold: 1)).Spies.ShouldBe(SpyTier.Lone);
    }

    [Fact]
    public void Clamp_steps_the_hunt_down_to_the_soldiers_left_at_home()
    {
        Order(attack: 9, hunt: HuntTier.Sweep).ClampTo(Kingdom(troops: 10)).Hunt.ShouldBe(HuntTier.Scout);
        Order(attack: 10, hunt: HuntTier.Scout).ClampTo(Kingdom(troops: 10)).Hunt.ShouldBeNull();
    }

    [Fact]
    public void A_clamped_order_always_fits()
    {
        var random = new Random(7);

        for (var i = 0; i < 2_000; i++)
        {
            var kingdom = Kingdom(
                gold: random.Next(0, 60),
                troops: random.Next(0, 20),
                villagers: random.Next(0, 20));

            var order = Order(
                attack: random.Next(0, 25),
                defend: random.Next(0, 25),
                trade: random.Next(0, 25),
                feast: random.Next(4) is var f and < 3 ? (FeastTier)f : null,
                buy: Enum.GetValues<PurchaseType>().Where(_ => random.Next(3) == 0).ToArray(),
                hired: random.Next(0, 4),
                armed: random.Next(0, 10),
                hunt: random.Next(4) is var h and < 3 ? (HuntTier)h : null);

            order.ClampTo(kingdom).FitsIn(kingdom).ShouldBeTrue($"case {i}: {kingdom}");
        }
    }
}
