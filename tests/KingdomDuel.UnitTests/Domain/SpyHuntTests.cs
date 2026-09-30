using KingdomDuel.Domain.Enums;
using KingdomDuel.Domain.Rounds;
using static KingdomDuel.UnitTests.Domain.GameSetup;

namespace KingdomDuel.UnitTests.Domain;

public sealed class SpyHuntTests
{
    [Fact]
    public void A_patrol_takes_a_ring_of_five_down_to_one()
    {
        var outcome = Resolve(
            Side(order: Order(hunt: HuntTier.Patrol)),
            Side(Kingdom(villagers: 10), spies: new SpyRing(SpyTier.Ring)));

        outcome.Player2.Spies.ShouldBe(new SpyRing(SpyTier.Lone));
        outcome.Count(RoundEventKind.SpiesHunted, 1).ShouldBe(4);
        outcome.Count(RoundEventKind.SpiesKilled, 2).ShouldBe(4);
    }

    [Fact]
    public void Caught_spies_never_come_back_to_the_village()
    {
        var outcome = Resolve(
            Side(order: Order(hunt: HuntTier.Sweep)),
            Side(Kingdom(villagers: 12), spies: new SpyRing(SpyTier.Pair)));

        outcome.Player2.Spies.ShouldBeNull();
        outcome.Player2.Kingdom.Villagers.ShouldBe(12);
    }

    [Fact]
    public void An_empty_search_is_reported_too()
    {
        var outcome = Resolve(Side(order: Order(hunt: HuntTier.Scout)), Side());

        outcome.Has(RoundEventKind.SpiesHunted, 1).ShouldBeTrue();
        outcome.Count(RoundEventKind.SpiesHunted, 1).ShouldBe(0);
        outcome.Has(RoundEventKind.SpiesKilled, 2).ShouldBeFalse();
    }

    [Fact]
    public void The_search_finds_spies_sent_in_the_same_round()
    {
        var outcome = Resolve(
            Side(order: Order(hunt: HuntTier.Sweep)),
            Side(Kingdom(villagers: 15), Order(spies: SpyTier.Ring)));

        outcome.Player2.Spies.ShouldBeNull();
        outcome.Player2.Kingdom.Villagers.ShouldBe(10);
    }

    [Theory]
    [InlineData(HuntTier.Scout, 1)]
    [InlineData(HuntTier.Patrol, 2)]
    [InlineData(HuntTier.Sweep, 5)]
    public void The_search_is_paid_in_gold(HuntTier tier, int cost)
    {
        Resolve(Side(Kingdom(gold: 50), Order(hunt: tier))).Player1.Kingdom.Gold.ShouldBe(50 - cost);
    }

    [Fact]
    public void Searching_soldiers_do_not_stand_in_the_keep()
    {
        var replay = GameEngine.PlanAssault(
            1,
            5,
            Kingdom(),
            new SideOutcome(Kingdom(troops: 10)),
            Order(hunt: HuntTier.Sweep),
            new Random(1));

        replay.DefenderInKeep.ShouldBe(7);
    }

    [Fact]
    public void Searching_soldiers_come_back_when_the_castle_holds()
    {
        var outcome = Play(
            Side(Kingdom(troops: 10), Order(attack: 5)),
            Side(Kingdom(troops: 10), Order(hunt: HuntTier.Sweep)),
            random: new FixedRandom(FixedRandom.FirstLoses));

        outcome.GameOverReason.ShouldBe(GameOverReason.None);
        outcome.Player2.Kingdom.Troops.ShouldBe(10);
    }

    [Fact]
    public void Searching_soldiers_fall_with_the_castle()
    {
        var outcome = Play(
            Side(Kingdom(troops: 20), Order(attack: 20)),
            Side(Kingdom(troops: 10), Order(hunt: HuntTier.Sweep)),
            random: new FixedRandom(FixedRandom.SecondLoses));

        outcome.GameOverReason.ShouldBe(GameOverReason.Player2CastleDestroyed);
        outcome.Player2.Kingdom.Troops.ShouldBe(0);
        outcome.Count(RoundEventKind.BattleLosses, 2).ShouldBe(10);
    }

    [Fact]
    public void Searching_soldiers_are_safe_when_the_wall_holds()
    {
        var outcome = Play(
            Side(Kingdom(troops: 10), Order(attack: 2)),
            Side(Kingdom(troops: 10), Order(defend: 6, hunt: HuntTier.Sweep)),
            random: new FixedRandom(FixedRandom.FirstLoses));

        outcome.Player2.Kingdom.Troops.ShouldBe(10);
    }
}
