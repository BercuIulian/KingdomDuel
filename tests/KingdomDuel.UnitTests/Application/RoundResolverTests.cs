using KingdomDuel.Application.Features.Matches;
using KingdomDuel.Domain;
using KingdomDuel.Domain.Entities.Matches;
using KingdomDuel.Domain.Enums;
using KingdomDuel.Domain.Rounds;
using KingdomDuel.UnitTests.Domain;
using static KingdomDuel.UnitTests.Application.MatchSetup;
using static KingdomDuel.UnitTests.Domain.GameSetup;

namespace KingdomDuel.UnitTests.Application;

public sealed class RoundResolverTests
{
    private readonly MatchSetup setup = new();

    [Fact]
    public void The_round_waits_for_both_orders()
    {
        var match = setup.InProgress();
        Orders(match, OrderPlan.Empty, null);

        setup.Resolver().ResolveIfReady(match).ShouldBeFalse();

        match.Round.ShouldBe(1);
        match.History.ShouldBeEmpty();
    }

    [Fact]
    public void A_quiet_round_is_recorded_and_the_next_one_starts()
    {
        var match = setup.InProgress();
        Orders(match, Order(trade: 5), OrderPlan.Empty);

        setup.Resolver().ResolveIfReady(match).ShouldBeTrue();

        match.Round.ShouldBe(2);
        match.History.Single().RoundNumber.ShouldBe(1);
        match.Player1!.PendingOrder.ShouldBeNull();
        match.Player2!.PendingOrder.ShouldBeNull();
        match.Player1.Caravan.ShouldNotBeNull();
        match.RoundDeadline.ShouldBe(setup.Now.AddSeconds(match.RoundSeconds));
    }

    [Fact]
    public void An_attack_holds_the_round_open_for_the_battle()
    {
        var match = setup.InProgress();
        Orders(match, Order(attack: 5), OrderPlan.Empty);

        var resolver = setup.Resolver();
        resolver.ResolveIfReady(match).ShouldBeTrue();

        match.LiveBattle.ShouldNotBeNull();
        match.LiveBattle.StartedAt.ShouldBe(setup.Now);
        match.RoundDeadline.ShouldBeNull();
        match.History.ShouldBeEmpty();
        resolver.ResolveIfReady(match).ShouldBeFalse();
    }

    [Fact]
    public void The_battle_settles_only_once_it_is_over()
    {
        var match = setup.InProgress();
        Orders(match, Order(attack: 3), Order(defend: 6));

        var resolver = setup.Resolver(new FixedRandom(FixedRandom.FirstLoses));
        resolver.ResolveIfReady(match);
        var duration = match.LiveBattle!.EndsAt;

        setup.Wait(duration - 0.1);
        resolver.SettleIfDue(match).ShouldBeFalse();
        match.LiveBattle.ShouldNotBeNull();

        setup.Wait(0.1);
        resolver.SettleIfDue(match).ShouldBeTrue();
        match.LiveBattle.ShouldBeNull();
        match.Round.ShouldBe(2);
        match.Player1!.Kingdom.Troops.ShouldBe(7);
    }

    [Fact]
    public void The_field_winner_is_asked_whether_to_march_on()
    {
        var match = FieldWonByPlayer1(out _);

        match.MarchChoice.ShouldNotBeNull();
        match.MarchChoice.Side.ShouldBe(1);
        match.MarchChoice.Army.ShouldBe(5);
        match.MarchChoice.Until.ShouldBe(setup.Now.AddSeconds(GameConstants.MarchChoiceSeconds));
        match.History.ShouldBeEmpty();
    }

    [Fact]
    public void Only_the_field_winner_decides_the_march()
    {
        var match = FieldWonByPlayer1(out var resolver);

        resolver.March(match, 2, advance: true).ShouldBeFalse();
        match.MarchChoice.ShouldNotBeNull();
    }

    [Fact]
    public void Turning_back_closes_the_round()
    {
        var match = FieldWonByPlayer1(out var resolver);

        resolver.March(match, 1, advance: false).ShouldBeTrue();

        match.MarchChoice.ShouldBeNull();
        match.Round.ShouldBe(2);
        match.History.Single().Events.ShouldContain(e => e.Kind == RoundEventKind.TurnedBack && e.Side == 1);
    }

    [Fact]
    public void An_army_left_waiting_turns_back_on_its_own()
    {
        var match = FieldWonByPlayer1(out var resolver);

        setup.Wait(GameConstants.MarchChoiceSeconds);

        resolver.March(match, 1, advance: true).ShouldBeFalse();
        resolver.SettleIfDue(match).ShouldBeTrue();
        match.MarchChoice.ShouldBeNull();
        match.Round.ShouldBe(2);
    }

    [Fact]
    public void Marching_on_starts_an_assault_with_the_army_left_standing()
    {
        var match = FieldWonByPlayer1(out var resolver);

        resolver.March(match, 1, advance: true).ShouldBeTrue();

        match.MarchChoice.ShouldBeNull();
        match.LiveBattle.ShouldNotBeNull();
        match.LiveBattle.Replay.AttackerSide.ShouldBe(1);
        match.LiveBattle.Replay.Player1Start.ShouldBe(5);
        match.LiveBattle.BeforeBattle.Events.ShouldContain(e => e.Kind == RoundEventKind.MarchedOn && e.Side == 1);
    }

    [Fact]
    public void Recruits_paid_this_round_do_not_defend_against_the_march()
    {
        var match = FieldWonByPlayer1(out var resolver, recruitsFor2: true);

        resolver.March(match, 1, advance: true);

        match.LiveBattle!.Replay.DefenderInKeep.ShouldBe(5);
    }

    [Fact]
    public void Recruits_arrive_once_the_assault_after_the_field_is_over()
    {
        var match = FieldWonByPlayer1(out _, recruitsFor2: true);
        var keepHolds = setup.Resolver(new FixedRandom(FixedRandom.FirstLoses));

        keepHolds.March(match, 1, advance: true);
        setup.Wait(match.LiveBattle!.EndsAt);
        keepHolds.SettleIfDue(match);
        setup.Wait(GameConstants.RallySeconds);
        keepHolds.SettleIfDue(match);
        setup.Wait(match.LiveBattle!.EndsAt - match.LiveBattle.Replay.BreachAt!.Value);
        keepHolds.SettleIfDue(match);

        match.LiveBattle.ShouldBeNull();
        match.Round.ShouldBe(2);
        match.Player2!.Kingdom.Troops.ShouldBe(5 + 6);
    }

    [Fact]
    public void Recruits_arrive_when_the_field_winner_turns_back()
    {
        var match = FieldWonByPlayer1(out var resolver, recruitsFor2: true);

        resolver.March(match, 1, advance: false);

        match.Player2!.Kingdom.Troops.ShouldBe(5 + 6);
    }

    [Fact]
    public void A_breach_rings_the_bell_for_the_defender()
    {
        var match = BreachedByPlayer1(out _);

        match.BreachCall.ShouldNotBeNull();
        match.BreachCall.Side.ShouldBe(2);
        match.BreachCall.Most.ShouldBe(15);
        match.BreachCall.Until.ShouldBe(setup.Now.AddSeconds(GameConstants.RallySeconds));
        match.LiveBattle.ShouldNotBeNull();
    }

    [Fact]
    public void Only_the_defender_answers_the_bell_and_only_while_it_rings()
    {
        var match = BreachedByPlayer1(out var resolver);

        resolver.Rally(match, 1, 5).ShouldBeFalse();

        setup.Wait(GameConstants.RallySeconds);
        resolver.Rally(match, 2, 5).ShouldBeFalse();
    }

    [Fact]
    public void Rallied_villagers_join_the_keep_and_the_assault_goes_on()
    {
        var match = BreachedByPlayer1(out var resolver);

        resolver.Rally(match, 2, 99).ShouldBeTrue();

        match.BreachCall.ShouldBeNull();
        match.LiveBattle!.Replay.DefenderInKeep.ShouldBe(10 + 15);
        match.LiveBattle.BeforeBattle.Player2.Kingdom.Gold.ShouldBe(50 - 30);
        match.LiveBattle.StartedAt.ShouldBe(setup.Now.AddSeconds(-match.LiveBattle.Replay.BreachAt!.Value));
    }

    [Fact]
    public void Nobody_answers_a_bell_left_ringing()
    {
        var match = BreachedByPlayer1(out var resolver);

        setup.Wait(GameConstants.RallySeconds);

        resolver.SettleIfDue(match).ShouldBeTrue();
        match.BreachCall.ShouldBeNull();
        match.LiveBattle!.Replay.DefenderInKeep.ShouldBe(10);
    }

    [Fact]
    public void The_bell_does_not_ring_for_a_village_that_cannot_answer()
    {
        var match = BreachedByPlayer1(out _, defenderGold: 1);

        match.BreachCall.ShouldBeNull();
        match.LiveBattle!.Replay.DefenderInKeep.ShouldBe(10);
    }

    [Fact]
    public void A_retreat_is_signalled_once_at_the_moment_it_is_called()
    {
        var match = setup.InProgress();
        Orders(match, Order(attack: 10), Order(attack: 5));

        var resolver = setup.Resolver(new FixedRandom(FixedRandom.FirstLoses));
        resolver.ResolveIfReady(match);

        setup.Wait(2);
        resolver.SignalRetreat(match, 1).ShouldBeTrue();
        match.LiveBattle!.Retreat.ShouldBe(new Retreat(1, 2));
        match.LiveBattle.EndsAt.ShouldBe(2 + GameConstants.RetreatSeconds);

        resolver.SignalRetreat(match, 1).ShouldBeFalse();
        resolver.SignalRetreat(match, 2).ShouldBeFalse();
    }

    [Fact]
    public void The_defender_cannot_retreat_from_its_own_walls()
    {
        var match = setup.InProgress();
        Orders(match, Order(attack: 5), OrderPlan.Empty);

        var resolver = setup.Resolver();
        resolver.ResolveIfReady(match);

        resolver.SignalRetreat(match, 2).ShouldBeFalse();
        resolver.SignalRetreat(match, 1).ShouldBeTrue();
    }

    [Fact]
    public void There_is_no_retreat_from_a_battle_already_over()
    {
        var match = setup.InProgress();
        Orders(match, Order(attack: 5), Order(attack: 5));

        var resolver = setup.Resolver(new FixedRandom(FixedRandom.FirstLoses));
        resolver.ResolveIfReady(match);
        setup.Wait(match.LiveBattle!.EndsAt);

        resolver.SignalRetreat(match, 1).ShouldBeFalse();
    }

    [Fact]
    public void The_round_does_not_expire_during_the_grace_seconds()
    {
        var match = setup.InProgress();
        Orders(match, OrderPlan.Empty, null);

        setup.Wait(match.RoundSeconds + GameConstants.RoundGraceSeconds);

        setup.Resolver().ExpireIfDue(match).ShouldBeFalse();
    }

    [Fact]
    public void An_expired_round_is_played_with_an_empty_order_for_whoever_missed_it()
    {
        var match = setup.InProgress();
        Orders(match, Order(trade: 5), null);

        setup.Wait(match.RoundSeconds + GameConstants.RoundGraceSeconds + 0.1);

        setup.Resolver().ExpireIfDue(match).ShouldBeTrue();
        match.Round.ShouldBe(2);
        match.Player1!.MissedRounds.ShouldBe(0);
        match.Player2!.MissedRounds.ShouldBe(1);
    }

    [Theory]
    [InlineData(2, 0, GameOverReason.Player1Disconnected)]
    [InlineData(0, 2, GameOverReason.Player2Disconnected)]
    [InlineData(2, 2, GameOverReason.BothDisconnected)]
    public void Three_silent_rounds_forfeit_the_match(int missed1, int missed2, GameOverReason reason)
    {
        var match = setup.InProgress();
        match.Player1!.MissedRounds = missed1;
        match.Player2!.MissedRounds = missed2;
        Orders(match, missed1 == 0 ? OrderPlan.Empty : null, missed2 == 0 ? OrderPlan.Empty : null);

        setup.Wait(match.RoundSeconds + GameConstants.RoundGraceSeconds + 1);
        setup.Resolver().ExpireIfDue(match);

        match.GameOverReason.ShouldBe(reason);
        match.Status.ShouldBe(MatchStatus.Completed);
        match.RoundDeadline.ShouldBeNull();
    }

    [Fact]
    public void The_round_clock_stops_while_a_battle_is_fought()
    {
        var match = setup.InProgress();
        Orders(match, Order(attack: 5), OrderPlan.Empty);
        var resolver = setup.Resolver();
        resolver.ResolveIfReady(match);
        match.RoundDeadline = setup.Now;

        setup.Wait(match.RoundSeconds);

        resolver.ExpireIfDue(match).ShouldBeFalse();
    }

    [Fact]
    public void The_last_round_completes_the_match()
    {
        var match = setup.InProgress(round: 10, totalRounds: 10);
        Orders(match, OrderPlan.Empty, OrderPlan.Empty);

        setup.Resolver().ResolveIfReady(match);

        match.Status.ShouldBe(MatchStatus.Completed);
        match.GameOverReason.ShouldBe(GameOverReason.RoundsExhaustedDraw);
        match.Round.ShouldBe(10);
        match.RoundDeadline.ShouldBeNull();
    }

    [Fact]
    public void Spies_bring_their_report_at_the_end_of_the_round()
    {
        var match = setup.InProgress();
        match.Player1!.Spies = new SpyRing(SpyTier.Ring);
        Orders(match, OrderPlan.Empty, Order(defend: 4));

        setup.Resolver().ResolveIfReady(match);

        match.Player1.Report.ShouldNotBeNull();
        match.Player1.Report.Round.ShouldBe(1);
        match.Player1.Report.Orders!.Defenders.ShouldBe(4);
        match.Player2!.Report.ShouldBeNull();
    }

    [Fact]
    public void The_round_clock_starts_from_now()
    {
        var match = setup.InProgress();
        match.RoundSeconds = 30;

        setup.Resolver().StartRoundClock(match);

        match.RoundDeadline.ShouldBe(setup.Now.AddSeconds(30));
    }

    private Match FieldWonByPlayer1(out RoundResolver resolver, bool recruitsFor2 = false)
    {
        var match = setup.InProgress(player2: Kingdom(gold: 50, troops: 10));
        Orders(
            match,
            Order(attack: 5),
            recruitsFor2 ? Order(attack: 5, buy: [PurchaseType.Recruit]) : Order(attack: 5));

        resolver = setup.Resolver(new FixedRandom(FixedRandom.SecondLoses));
        resolver.ResolveIfReady(match);
        setup.Wait(match.LiveBattle!.EndsAt);
        resolver.SettleIfDue(match);

        return match;
    }

    private Match BreachedByPlayer1(out RoundResolver resolver, int defenderGold = 50)
    {
        var match = setup.InProgress(player2: Kingdom(gold: defenderGold, troops: 10, villagers: 15));
        Orders(match, Order(attack: 10), OrderPlan.Empty);

        resolver = setup.Resolver(new FixedRandom(FixedRandom.FirstLoses));
        resolver.ResolveIfReady(match);
        setup.Wait(match.LiveBattle!.EndsAt);
        resolver.SettleIfDue(match);

        return match;
    }
}
