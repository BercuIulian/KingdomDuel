using KingdomDuel.Application.Features.Matches;
using KingdomDuel.Domain.Entities.Matches;
using KingdomDuel.Domain.Enums;
using KingdomDuel.Domain.Rounds;
using static KingdomDuel.UnitTests.Application.MatchSetup;
using static KingdomDuel.UnitTests.Domain.GameSetup;

namespace KingdomDuel.UnitTests.Application;

public sealed class MatchRecorderTests
{
    private readonly MatchSetup setup = new();

    [Fact]
    public void A_match_still_in_play_is_not_recorded()
    {
        MatchRecorder.EntriesFor(setup.InProgress(), setup.Now).ShouldBeEmpty();
    }

    [Fact]
    public void A_finished_match_is_recorded_once_for_each_player()
    {
        var match = setup.InProgress(round: 7);
        match.Status = MatchStatus.Completed;
        match.GameOverReason = GameOverReason.Player2Left;

        var entries = MatchRecorder.EntriesFor(match, setup.Now);

        entries.Count.ShouldBe(2);

        var host = entries.Single(e => e.UserId == Host);
        host.Outcome.ShouldBe(MatchOutcome.Won);
        host.OpponentName.ShouldBe("oaspete");
        host.RoundsPlayed.ShouldBe(7);
        host.StartedAt.ShouldBe(Start);

        var guest = entries.Single(e => e.UserId == Guest);
        guest.Outcome.ShouldBe(MatchOutcome.Lost);
        guest.OpponentName.ShouldBe("gazda");
    }

    [Fact]
    public void A_match_without_a_start_time_is_dated_now()
    {
        var match = setup.InProgress();
        match.Status = MatchStatus.Completed;
        match.StartedAt = null;

        MatchRecorder.EntriesFor(match, setup.Now.AddHours(1))[0].StartedAt.ShouldBe(Start.AddHours(1));
    }

    [Fact]
    public void The_recorded_power_is_the_power_that_decided_the_match()
    {
        var match = LastRoundWonByTheCaravan();

        var host = MatchRecorder.EntriesFor(match, setup.Now).Single(e => e.UserId == Host);

        host.Outcome.ShouldBe(MatchOutcome.Won);
        host.YourPower.ShouldBeGreaterThan(host.OpponentPower);
    }

    [Fact]
    public void The_story_shows_the_power_that_decided_the_match()
    {
        var match = LastRoundWonByTheCaravan();

        var last = match.ToStateDto(Host, setup.Now).Story!.Rounds[^1];

        last.Player1Power.ShouldBeGreaterThan(last.Player2Power);
    }

    private Match LastRoundWonByTheCaravan()
    {
        var match = setup.InProgress(
            Kingdom(gold: 50, villagers: 5),
            Kingdom(gold: 50, villagers: 14),
            round: 10,
            totalRounds: 10);
        match.Player1!.Caravan = new Caravan(10, 12);
        Orders(match, OrderPlan.Empty, OrderPlan.Empty);

        setup.Resolver().ResolveIfReady(match);
        match.GameOverReason.ShouldBe(GameOverReason.RoundsExhaustedPlayer1Wins);

        return match;
    }
}
