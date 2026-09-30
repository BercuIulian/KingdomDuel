using KingdomDuel.Domain.Enums;
using KingdomDuel.Domain.Rounds;
using static KingdomDuel.UnitTests.Domain.GameSetup;

namespace KingdomDuel.UnitTests.Domain;

public sealed class FieldBattleTests
{
    [Fact]
    public void Two_armies_on_the_road_meet_in_the_field()
    {
        var outcome = Resolve(Side(order: Order(attack: 5)), Side(order: Order(attack: 4)));

        outcome.Battle.ShouldNotBeNull();
        outcome.Battle.AttackerSide.ShouldBe(0);
        outcome.Battle.Player1Start.ShouldBe(5);
        outcome.Battle.Player2Start.ShouldBe(4);
        outcome.Battle.BreachAt.ShouldBeNull();
    }

    [Fact]
    public void The_field_winner_gains_morale_and_the_loser_loses_it()
    {
        var outcome = Play(
            Side(Kingdom(troops: 10, morale: 50), Order(attack: 5)),
            Side(Kingdom(troops: 10, morale: 50), Order(attack: 5)),
            random: new FixedRandom(FixedRandom.SecondLoses));

        outcome.Player1.Kingdom.Troops.ShouldBe(10);
        outcome.Player2.Kingdom.Troops.ShouldBe(5);
        outcome.Player1.Kingdom.Morale.ShouldBe(47 + 3);
        outcome.Player2.Kingdom.Morale.ShouldBe(47 - 5);
    }

    [Fact]
    public void Retreating_breaks_off_in_three_seconds_and_leaves_a_tenth_behind()
    {
        var random = new FixedRandom(FixedRandom.FirstLoses);
        var before = Resolve(
            Side(Kingdom(troops: 10), Order(attack: 10)),
            Side(Kingdom(troops: 10), Order(attack: 5)),
            random: random);

        // one soldier of the first army falls every second; five are down when the retreat ends
        var outcome = Settle(before, before.Battle!, 1, 10, random, retreat: new Retreat(1, 2));

        outcome.Count(RoundEventKind.Retreated, 1).ShouldBe(1);
        outcome.Player1.Kingdom.Troops.ShouldBe(10 - 5 - 1);
        outcome.Player2.Kingdom.Troops.ShouldBe(10);
        outcome.Player1.Kingdom.Morale.ShouldBe(47 - 5);
        outcome.Player2.Kingdom.Morale.ShouldBe(47 + 3);
    }

    [Fact]
    public void Catapults_are_lost_when_the_army_retreats()
    {
        var random = new FixedRandom(FixedRandom.FirstLoses);
        var before = Resolve(
            Side(Kingdom(troops: 10, catapults: 2), Order(attack: 10)),
            Side(Kingdom(troops: 10), Order(attack: 5)),
            random: random);

        var outcome = Settle(before, before.Battle!, 1, 10, random, retreat: new Retreat(1, 2));

        outcome.Player1.Kingdom.Catapults.ShouldBe(0);
        outcome.Count(RoundEventKind.CatapultsLost, 1).ShouldBe(2);
    }

    [Fact]
    public void Catapults_are_lost_when_the_army_is_wiped_out()
    {
        var outcome = Play(
            Side(Kingdom(troops: 10, catapults: 2), Order(attack: 5)),
            Side(Kingdom(troops: 10), Order(attack: 5)),
            random: new FixedRandom(FixedRandom.FirstLoses));

        outcome.Player1.Kingdom.Catapults.ShouldBe(0);
    }

    [Fact]
    public void Catapults_stay_with_an_army_that_wins_the_field()
    {
        var outcome = Play(
            Side(Kingdom(troops: 10, catapults: 2), Order(attack: 5)),
            Side(Kingdom(troops: 10), Order(attack: 5)),
            random: new FixedRandom(FixedRandom.SecondLoses));

        outcome.Player1.Kingdom.Catapults.ShouldBe(2);
    }

    [Fact]
    public void The_army_left_standing_chooses_whether_to_march_on()
    {
        var outcome = Resolve(
            Side(order: Order(attack: 5)),
            Side(order: Order(attack: 5)),
            random: new FixedRandom(FixedRandom.SecondLoses));

        var replay = outcome.Battle!;

        GameEngine.MarchChoiceSide(replay, replay.Duration, null).ShouldBe(1);
        GameEngine.SurvivingExpedition(replay, replay.Duration, 1).ShouldBe(5);
    }

    [Fact]
    public void After_a_retreat_the_other_army_chooses()
    {
        var outcome = Resolve(
            Side(order: Order(attack: 10)),
            Side(order: Order(attack: 5)),
            random: new FixedRandom(FixedRandom.FirstLoses));

        GameEngine.MarchChoiceSide(outcome.Battle!, 5, new Retreat(1, 2)).ShouldBe(2);
    }

    [Fact]
    public void Nobody_chooses_after_an_assault()
    {
        var outcome = Resolve(Side(order: Order(attack: 5)), Side());

        GameEngine.MarchChoiceSide(outcome.Battle!, outcome.Battle!.Duration, null).ShouldBe(0);
    }

    [Fact]
    public void Both_sides_may_retreat_from_the_field_once()
    {
        var outcome = Resolve(Side(order: Order(attack: 5)), Side(order: Order(attack: 5)));
        var battle = new LiveBattle(outcome.Battle!, outcome, DateTimeOffset.UnixEpoch);

        battle.CanRetreat(1).ShouldBeTrue();
        battle.CanRetreat(2).ShouldBeTrue();
        battle.CanRetreat(0).ShouldBeFalse();

        (battle with { Retreat = new Retreat(1, 1) }).CanRetreat(2).ShouldBeFalse();
    }

    [Fact]
    public void Only_the_attacker_may_retreat_from_an_assault()
    {
        var outcome = Resolve(Side(), Side(order: Order(attack: 5)));
        var battle = new LiveBattle(outcome.Battle!, outcome, DateTimeOffset.UnixEpoch);

        battle.CanRetreat(2).ShouldBeTrue();
        battle.CanRetreat(1).ShouldBeFalse();
    }

    [Fact]
    public void A_retreat_ends_the_battle_three_seconds_after_the_signal()
    {
        var outcome = Resolve(
            Side(order: Order(attack: 10)),
            Side(order: Order(attack: 5)),
            random: new FixedRandom(FixedRandom.FirstLoses));

        var battle = new LiveBattle(outcome.Battle!, outcome, DateTimeOffset.UnixEpoch, new Retreat(1, 2));

        battle.EndsAt.ShouldBe(5);
        battle.IsOverAt(DateTimeOffset.UnixEpoch.AddSeconds(4.9)).ShouldBeFalse();
        battle.IsOverAt(DateTimeOffset.UnixEpoch.AddSeconds(5)).ShouldBeTrue();
    }
}
