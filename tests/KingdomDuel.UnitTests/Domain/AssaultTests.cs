using KingdomDuel.Domain;
using KingdomDuel.Domain.Enums;
using KingdomDuel.Domain.Rounds;
using static KingdomDuel.UnitTests.Domain.GameSetup;

namespace KingdomDuel.UnitTests.Domain;

public sealed class AssaultTests
{
    [Fact]
    public void A_lone_army_marches_on_the_castle()
    {
        var outcome = Resolve(Side(order: Order(attack: 5)), Side(order: Order(defend: 3)));

        outcome.Battle!.AttackerSide.ShouldBe(1);
        outcome.Battle.Player1Start.ShouldBe(5);
        outcome.Battle.Player2Start.ShouldBe(3);
    }

    [Theory]
    [InlineData(0, 6)]
    [InlineData(1, 8)]
    [InlineData(2, 10)]
    public void Only_as_many_archers_as_the_wall_holds_go_up(int fortification, int onTheWall)
    {
        var outcome = Resolve(
            Side(order: Order(attack: 5)),
            Side(Kingdom(troops: 20, fortification: fortification), Order(defend: 20)));

        outcome.Battle!.Player2Start.ShouldBe(onTheWall);
    }

    [Fact]
    public void Nobody_climbs_a_ruined_wall_and_the_assault_goes_straight_in()
    {
        var outcome = Resolve(
            Side(order: Order(attack: 5)),
            Side(Kingdom(troops: 10, fortification: -1), Order(defend: 6)));

        outcome.Battle!.Player2Start.ShouldBe(0);
        outcome.Battle.BreachAt.ShouldBe(0);
        outcome.Battle.DefenderInKeep.ShouldBe(10);
    }

    [Fact]
    public void An_army_that_falls_on_the_walls_leaves_them_standing()
    {
        var outcome = Play(
            Side(Kingdom(troops: 10, catapults: 1), Order(attack: 3)),
            Side(Kingdom(troops: 10, fortification: 1), Order(defend: 6)),
            random: new FixedRandom(FixedRandom.FirstLoses));

        outcome.GameOverReason.ShouldBe(GameOverReason.None);
        outcome.Player1.Kingdom.Troops.ShouldBe(7);
        outcome.Player1.Kingdom.Catapults.ShouldBe(0);
        outcome.Player2.Kingdom.Troops.ShouldBe(10);
        outcome.Player2.Kingdom.Fortification.ShouldBe(1);
        outcome.Has(RoundEventKind.WallsHeld, 2).ShouldBeTrue();
        outcome.Player1.Kingdom.Morale.ShouldBe(47 - 5);
        outcome.Player2.Kingdom.Morale.ShouldBe(47 + 3);
    }

    [Fact]
    public void The_wall_breaks_open_the_keep()
    {
        var outcome = Resolve(
            Side(order: Order(attack: 10)),
            Side(Kingdom(troops: 10), Order(defend: 6)),
            random: new FixedRandom(FixedRandom.SecondLoses));

        var replay = outcome.Battle!;

        replay.BreachAt.ShouldBe(GameConstants.WallBreakingSeconds);
        replay.DefenderInKeep.ShouldBe(4);
        GameEngine.KeepToCome(replay, replay.Duration, null).ShouldBeTrue();
        GameEngine.KeepToCome(replay, replay.Duration, new Retreat(1, 1)).ShouldBeFalse();
    }

    [Fact]
    public void Archers_left_on_a_breached_wall_fall_back_into_the_keep()
    {
        var replay = GameEngine.PlanAssault(
            1,
            30,
            Kingdom(),
            new SideOutcome(Kingdom(troops: 10)),
            Order(defend: 6),
            new FixedRandom(FixedRandom.FirstLoses));

        replay.BreachAt.ShouldBe(15);
        replay.DefenderInKeep.ShouldBe(4 + Battle.FallBack(6));
    }

    [Fact]
    public void A_castle_left_without_soldiers_falls_and_ends_the_match()
    {
        var outcome = Play(
            Side(order: Order(attack: 10)),
            Side(Kingdom(troops: 10), Order(defend: 6)),
            random: new FixedRandom(FixedRandom.SecondLoses));

        outcome.GameOverReason.ShouldBe(GameOverReason.Player2CastleDestroyed);
        outcome.Has(RoundEventKind.CastleDestroyed, 2).ShouldBeTrue();
        outcome.Player2.Kingdom.Troops.ShouldBe(0);
    }

    [Fact]
    public void A_breached_wall_stays_ruined_even_when_the_keep_holds()
    {
        var random = new FixedRandom(FixedRandom.SecondLoses);
        var before = Resolve(
            Side(order: Order(attack: 10)),
            Side(Kingdom(troops: 20, fortification: 2), Order(defend: 10)),
            random: random);

        var keepHolds = new FixedRandom(FixedRandom.FirstLoses);
        var outcome = Settle(before, before.Battle!, 1, 10, keepHolds);

        outcome.GameOverReason.ShouldBe(GameOverReason.None);
        outcome.Player2.Kingdom.Fortification.ShouldBe(GameConstants.WallDownLevel);
        outcome.Player2.Kingdom.RuinedFrom.ShouldBe(2);
        outcome.Count(RoundEventKind.WallsRuined, 2).ShouldBe(2);
        outcome.Player2.Kingdom.Morale.ShouldBe(47 + 3);
    }

    [Fact]
    public void A_ruined_wall_does_not_fall_twice()
    {
        var outcome = Play(
            Side(order: Order(attack: 3)),
            Side(Kingdom(troops: 20, fortification: -1)),
            random: new FixedRandom(FixedRandom.FirstLoses));

        outcome.Has(RoundEventKind.WallsRuined, 2).ShouldBeFalse();
    }

    [Theory]
    [InlineData(50, 15, 15)]
    [InlineData(5, 15, 2)]
    [InlineData(50, 3, 3)]
    public void The_village_answers_the_bell_as_far_as_gold_and_villagers_go(int gold, int villagers, int most)
    {
        GameEngine.RallyCeiling(Kingdom(gold: gold, villagers: villagers)).ShouldBe(most);
    }

    [Fact]
    public void Rallied_villagers_cost_two_gold_and_stay_soldiers()
    {
        var random = new FixedRandom(FixedRandom.SecondLoses);
        var before = Resolve(
            Side(order: Order(attack: 10)),
            Side(Kingdom(gold: 50, troops: 10, villagers: 15), Order(defend: 6)),
            random: random);

        var keepHolds = new FixedRandom(FixedRandom.FirstLoses);
        var outcome = Settle(before, before.Battle!, 1, 10, keepHolds, rallied: 5);

        outcome.GameOverReason.ShouldBe(GameOverReason.None);
        outcome.Player2.Kingdom.Gold.ShouldBe(40);
        outcome.Player2.Kingdom.Villagers.ShouldBe(10);
        outcome.Player2.Kingdom.Troops.ShouldBe(4 + 5);
        outcome.Count(RoundEventKind.VillagersRallied, 2).ShouldBe(5);
    }

    [Fact]
    public void Rallied_villagers_stand_in_the_keep()
    {
        var before = Resolve(
            Side(order: Order(attack: 10)),
            Side(Kingdom(troops: 10), Order(defend: 6)),
            random: new FixedRandom(FixedRandom.SecondLoses));

        var stormed = GameEngine.StormKeep(before.Battle!, 5, 50, 50, new FixedRandom(FixedRandom.FirstLoses));

        stormed.DefenderInKeep.ShouldBe(4 + 5);
    }
}
