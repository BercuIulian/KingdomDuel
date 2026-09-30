using KingdomDuel.Domain;
using KingdomDuel.Domain.Enums;
using KingdomDuel.Domain.Rounds;
using static KingdomDuel.UnitTests.Domain.GameSetup;

namespace KingdomDuel.UnitTests.Domain;

public sealed class SpyTests
{
    [Fact]
    public void Spies_leave_the_village_and_are_paid_from_the_first_round()
    {
        var outcome = Resolve(Side(Kingdom(gold: 50, villagers: 15), Order(spies: SpyTier.Pair)));

        outcome.Player1.Spies.ShouldBe(new SpyRing(SpyTier.Pair));
        outcome.Player1.Kingdom.Villagers.ShouldBe(12);
        outcome.Player1.Kingdom.Gold.ShouldBe(47);
    }

    [Fact]
    public void Spies_stay_away_round_after_round_and_keep_costing()
    {
        var outcome = Resolve(Side(Kingdom(gold: 50, villagers: 12), spies: new SpyRing(SpyTier.Pair)));

        outcome.Player1.Spies.ShouldBe(new SpyRing(SpyTier.Pair));
        outcome.Player1.Kingdom.Villagers.ShouldBe(12);
        outcome.Player1.Kingdom.Gold.ShouldBe(47);
    }

    [Fact]
    public void Raising_the_ring_sends_only_the_extra_villagers()
    {
        var outcome = Resolve(Side(
            Kingdom(gold: 50, villagers: 14),
            Order(spies: SpyTier.Ring),
            spies: new SpyRing(SpyTier.Lone)));

        outcome.Player1.Kingdom.Villagers.ShouldBe(10);
        outcome.Player1.Kingdom.Gold.ShouldBe(45);
    }

    [Fact]
    public void Lowering_the_ring_brings_the_rest_home()
    {
        var outcome = Resolve(Side(
            Kingdom(gold: 50, villagers: 10),
            Order(spies: SpyTier.Lone),
            spies: new SpyRing(SpyTier.Ring)));

        outcome.Player1.Kingdom.Villagers.ShouldBe(14);
        outcome.Player1.Kingdom.Gold.ShouldBe(49);
    }

    [Fact]
    public void A_ring_stays_whole_with_nobody_left_at_home()
    {
        var outcome = Resolve(Side(
            Kingdom(gold: 50, villagers: 0),
            Order(spies: SpyTier.Pair),
            spies: new SpyRing(SpyTier.Pair)));

        outcome.Player1.Spies.ShouldBe(new SpyRing(SpyTier.Pair));
        outcome.Player1.Kingdom.Gold.ShouldBe(47);
    }

    [Fact]
    public void A_ring_is_lowered_as_ordered_with_nobody_left_at_home()
    {
        var outcome = Resolve(Side(
            Kingdom(gold: 50, villagers: 0),
            Order(spies: SpyTier.Lone),
            spies: new SpyRing(SpyTier.Pair)));

        outcome.Player1.Spies.ShouldBe(new SpyRing(SpyTier.Lone));
        outcome.Player1.Kingdom.Villagers.ShouldBe(2);
        outcome.Player1.Kingdom.Gold.ShouldBe(49);
    }

    [Fact]
    public void Recalled_spies_come_home_and_cost_nothing()
    {
        var outcome = Resolve(Side(
            Kingdom(gold: 50, villagers: 12),
            Order(recallSpies: true),
            spies: new SpyRing(SpyTier.Pair)));

        outcome.Player1.Spies.ShouldBeNull();
        outcome.Player1.Kingdom.Villagers.ShouldBe(15);
        outcome.Player1.Kingdom.Gold.ShouldBe(50);
    }

    [Fact]
    public void Spies_come_home_on_their_own_when_the_gold_runs_out()
    {
        var outcome = Resolve(Side(Kingdom(gold: 2, villagers: 12), spies: new SpyRing(SpyTier.Pair)));

        outcome.Player1.Spies.ShouldBeNull();
        outcome.Player1.Kingdom.Villagers.ShouldBe(15);
        outcome.Player1.Kingdom.Gold.ShouldBe(2);
        outcome.Count(RoundEventKind.SpiesStarved, 1).ShouldBe(3);
    }

    [Fact]
    public void No_spies_means_no_report()
    {
        Spying.ReportOn(3, null, new SideOutcome(Kingdom()), null, new Random(1)).ShouldBeNull();
    }

    [Fact]
    public void Caravan_and_wall_are_reported_exactly()
    {
        var watched = new SideOutcome(Kingdom(fortification: 2), new Caravan(7, 5));

        var report = Spying.ReportOn(3, new SpyRing(SpyTier.Lone), watched, null, new FixedRandom(0))!;

        report.Round.ShouldBe(3);
        report.CaravanSent.ShouldBeTrue();
        report.Fortification.ShouldBe(2);
    }

    [Theory]
    [InlineData(SpyTier.Lone, 65, 135)]
    [InlineData(SpyTier.Pair, 85, 115)]
    [InlineData(SpyTier.Ring, 95, 105)]
    public void Numbers_are_blurred_within_the_accuracy_of_the_ring(SpyTier tier, int low, int high)
    {
        var accuracy = GameConstants.SpyAccuracy(tier);

        Spying.Blur(100, accuracy, new FixedRandom(0)).ShouldBe(low);
        Spying.Blur(100, accuracy, new FixedRandom(0.5)).ShouldBe(100);
        Spying.Blur(100, accuracy, new FixedRandom(0.9999999)).ShouldBe(high);
    }

    [Fact]
    public void Nothing_is_blurred_into_something()
    {
        Spying.Blur(0, 0.65, new FixedRandom(0.9999999)).ShouldBe(0);
    }

    [Theory]
    [InlineData(SpyTier.Lone)]
    [InlineData(SpyTier.Pair)]
    public void Smaller_rings_do_not_read_orders(SpyTier tier)
    {
        var report = Spying.ReportOn(3, new SpyRing(tier), new SideOutcome(Kingdom()), Order(defend: 4), new Random(1))!;

        report.Orders.ShouldBeNull();
    }

    [Fact]
    public void Five_spies_read_the_orders_exactly()
    {
        var order = Order(defend: 4, feast: FeastTier.Court, buy: [PurchaseType.Recruit], spies: SpyTier.Lone);

        var report = Spying.ReportOn(3, new SpyRing(SpyTier.Ring), new SideOutcome(Kingdom()), order, new Random(1))!;

        report.Orders.ShouldBe(new SpyOrders(FeastTier.Court, 4, true, true));
    }

    [Fact]
    public void Spies_left_alone_do_not_show_in_the_orders()
    {
        var report = Spying.ReportOn(3, new SpyRing(SpyTier.Ring), new SideOutcome(Kingdom()), OrderPlan.Empty, new Random(1))!;

        report.Orders!.TouchedSpies.ShouldBeFalse();
    }
}
