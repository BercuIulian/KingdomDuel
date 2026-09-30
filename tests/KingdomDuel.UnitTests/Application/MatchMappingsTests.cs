using KingdomDuel.Application.Features.Matches;
using KingdomDuel.Domain.Enums;
using KingdomDuel.Domain.Rounds;
using KingdomDuel.UnitTests.Domain;
using static KingdomDuel.UnitTests.Application.MatchSetup;
using static KingdomDuel.UnitTests.Domain.GameSetup;

namespace KingdomDuel.UnitTests.Application;

public sealed class MatchMappingsTests
{
    private readonly MatchSetup setup = new();

    [Fact]
    public void Each_player_sees_their_own_kingdom_and_not_the_other()
    {
        var match = setup.InProgress(Kingdom(gold: 70), Kingdom(gold: 30));

        var seen = match.ToStateDto(Host, setup.Now);

        seen.YourSide.ShouldBe(1);
        seen.IsHost.ShouldBeTrue();
        seen.Player1Kingdom!.Gold.ShouldBe(70);
        seen.Player2Kingdom.ShouldBeNull();

        var other = match.ToStateDto(Guest, setup.Now);

        other.IsHost.ShouldBeFalse();
        other.Player1Kingdom.ShouldBeNull();
        other.Player2Kingdom!.Gold.ShouldBe(30);
    }

    [Fact]
    public void Spies_replace_the_unknown_with_hearsay_and_never_the_gold()
    {
        var match = setup.InProgress(player2: Kingdom(gold: 30, catapults: 2));
        match.Player1!.Report = new SpyReport(1, SpyTier.Pair, false, 0, 1, 14, 9, 44, null);

        var heard = match.ToStateDto(Host, setup.Now).Player2Kingdom!;

        heard.IsHearsay.ShouldBeTrue();
        heard.Gold.ShouldBe(0);
        heard.TotalPower.ShouldBe(0);
        heard.Catapults.ShouldBe(0);
        heard.Troops.ShouldBe(9);
        heard.Villagers.ShouldBe(14);
        heard.Morale.ShouldBe(44);
        heard.Fortification.ShouldBe(1);
    }

    [Fact]
    public void Both_kingdoms_are_revealed_when_the_match_is_over()
    {
        var match = setup.InProgress(Kingdom(gold: 70), Kingdom(gold: 30));
        match.Status = MatchStatus.Completed;

        var seen = match.ToStateDto(Host, setup.Now);

        seen.Player2Kingdom!.Gold.ShouldBe(30);
        seen.Player2Kingdom.IsHearsay.ShouldBeFalse();
    }

    [Fact]
    public void A_stranger_sees_neither_kingdom()
    {
        var seen = setup.InProgress().ToStateDto(Stranger, setup.Now);

        seen.YourSide.ShouldBe(0);
        seen.Player1Kingdom.ShouldBeNull();
        seen.Player2Kingdom.ShouldBeNull();
        seen.LastRound.ShouldBeNull();
    }

    [Fact]
    public void Caravans_spies_and_reports_are_shown_only_to_their_owner()
    {
        var match = setup.InProgress();
        match.Player2!.Caravan = new Caravan(7, 3);
        match.Player2.Spies = new SpyRing(SpyTier.Lone);
        match.Player2.Report = new SpyReport(1, SpyTier.Lone, false, 0, 0, 15, 10, 50, null);

        var host = match.ToStateDto(Host, setup.Now);
        host.YourCaravan.ShouldBeNull();
        host.YourSpies.ShouldBeNull();
        host.YourReport.ShouldBeNull();

        var guest = match.ToStateDto(Guest, setup.Now);
        guest.YourCaravan!.Troops.ShouldBe(7);
        guest.YourSpies!.Tier.ShouldBe(SpyTier.Lone);
        guest.YourReport!.Round.ShouldBe(1);
    }

    [Fact]
    public void The_round_recap_tells_each_player_only_their_own_story()
    {
        var match = setup.InProgress(Kingdom(gold: 50, morale: 50), Kingdom(gold: 50, morale: 50));
        Orders(match, Order(feast: FeastTier.Small), Order(hunt: HuntTier.Scout));
        setup.Resolver().ResolveIfReady(match);

        var recap = match.ToStateDto(Host, setup.Now).LastRound!;

        recap.Round.ShouldBe(1);
        recap.GoldDelta.ShouldBe(-5);
        recap.MoraleDelta.ShouldBe(-3 + 4);
        recap.Lines.ShouldContain(line => line.Kind == RoundEventKind.Feast);
        recap.Lines.ShouldNotContain(line => line.Kind == RoundEventKind.SpiesHunted);
    }

    [Fact]
    public void The_story_of_the_match_is_told_only_at_the_end()
    {
        var match = setup.InProgress();
        Orders(match, OrderPlan.Empty, OrderPlan.Empty);
        setup.Resolver().ResolveIfReady(match);

        match.ToStateDto(Host, setup.Now).Story.ShouldBeNull();

        match.Status = MatchStatus.Completed;
        var story = match.ToStateDto(Host, setup.Now).Story!;

        story.Rounds.Select(r => r.Round).ShouldBe([0, 1]);
    }

    [Fact]
    public void The_attacker_sees_only_the_archers_until_the_wall_falls()
    {
        var match = setup.InProgress(player2: Kingdom(troops: 12));
        Orders(match, Order(attack: 10), Order(defend: 2));
        var resolver = setup.Resolver(new FixedRandom(FixedRandom.FirstLoses));
        resolver.ResolveIfReady(match);

        var breach = match.LiveBattle!.Replay.BreachAt!.Value;

        setup.Wait(breach - 1);
        var early = match.ToStateDto(Host, setup.Now).Battle!;
        early.Player2Start.ShouldBe(2);
        early.KeepDefenders.ShouldBeNull();

        setup.Wait(1);
        match.ToStateDto(Host, setup.Now).Battle!.KeepDefenders.ShouldBe(10 + 2);
    }

    [Fact]
    public void The_attacker_does_not_learn_how_many_can_answer_the_bell()
    {
        var match = setup.InProgress(player2: Kingdom(gold: 50, villagers: 15));
        Orders(match, Order(attack: 10), OrderPlan.Empty);
        var resolver = setup.Resolver(new FixedRandom(FixedRandom.FirstLoses));
        resolver.ResolveIfReady(match);
        setup.Wait(match.LiveBattle!.EndsAt);
        resolver.SettleIfDue(match);

        match.ToStateDto(Guest, setup.Now).Breach!.Most.ShouldBe(15);
        match.ToStateDto(Host, setup.Now).Breach!.Most.ShouldBe(0);
    }

    [Fact]
    public void Only_the_side_that_may_retreat_is_offered_the_retreat()
    {
        var match = setup.InProgress();
        Orders(match, Order(attack: 5), OrderPlan.Empty);
        setup.Resolver().ResolveIfReady(match);

        match.ToStateDto(Host, setup.Now).Battle!.CanRetreat.ShouldBeTrue();
        match.ToStateDto(Guest, setup.Now).Battle!.CanRetreat.ShouldBeFalse();

        setup.Wait(match.LiveBattle!.EndsAt);
        match.ToStateDto(Host, setup.Now).Battle!.CanRetreat.ShouldBeFalse();
    }

    [Fact]
    public void The_round_clock_rounds_up_and_stops_at_zero()
    {
        var match = setup.InProgress();
        match.RoundDeadline = setup.Now.AddSeconds(10.2);

        match.SecondsRemaining(setup.Now).ShouldBe(11);
        match.SecondsRemaining(setup.Now.AddSeconds(20)).ShouldBe(0);

        match.Status = MatchStatus.Completed;
        match.SecondsRemaining(setup.Now).ShouldBeNull();
    }
}
