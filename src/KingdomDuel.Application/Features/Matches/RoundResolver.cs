using KingdomDuel.Domain;
using KingdomDuel.Domain.Entities.Matches;
using KingdomDuel.Domain.Enums;
using KingdomDuel.Domain.Rounds;

namespace KingdomDuel.Application.Features.Matches;

internal static class RoundResolver
{
    public static bool ExpireIfDue(Match match)
    {
        if (match.Status != MatchStatus.InProgress
            || match.LiveBattle is not null
            || match.Player1 is null
            || match.Player2 is null
            || match.RoundDeadline is not { } deadline)
        {
            return false;
        }

        if (DateTimeOffset.UtcNow <= deadline.AddSeconds(GameConstants.RoundGraceSeconds))
        {
            return false;
        }

        var player1Missed = match.Player1.PendingOrder is null;
        var player2Missed = match.Player2.PendingOrder is null;

        match.Player1.PendingOrder ??= OrderPlan.Empty;
        match.Player2.PendingOrder ??= OrderPlan.Empty;

        if (!ResolveIfReady(match))
        {
            return false;
        }

        if (player1Missed)
        {
            match.Player1.MissedRounds += 1;
        }

        if (player2Missed)
        {
            match.Player2.MissedRounds += 1;
        }

        ForfeitIfAbandoned(match);

        return true;
    }

    public static bool ResolveIfReady(Match match)
    {
        if (match.Status != MatchStatus.InProgress
            || match.LiveBattle is not null
            || match.Player1 is null
            || match.Player2 is null)
        {
            return false;
        }

        if (match.Player1.PendingOrder is not { } order1 || match.Player2.PendingOrder is not { } order2)
        {
            return false;
        }

        var outcome = GameEngine.Resolve(new RoundInput(
            match.Round,
            match.TotalRounds,
            match.CurrentEvent,
            new SideInput(match.Player1.Kingdom, order1, match.Player1.Caravan),
            new SideInput(match.Player2.Kingdom, order2, match.Player2.Caravan)),
            Random.Shared);

        if (outcome.Battle is { } battle)
        {
            match.LiveBattle = new LiveBattle(battle, outcome, DateTimeOffset.UtcNow);
            match.RoundDeadline = null;

            return true;
        }

        Apply(match, outcome);

        return true;
    }

    public static bool SettleIfDue(Match match)
    {
        if (match.LiveBattle is not { } battle || !battle.IsOverAt(DateTimeOffset.UtcNow))
        {
            return false;
        }

        match.LiveBattle = null;
        Apply(match, GameEngine.SettleBattle(
            battle.BeforeBattle,
            battle.Replay,
            battle.EndsAt,
            match.Round,
            match.TotalRounds,
            battle.Retreat));

        return true;
    }

    public static bool SignalRetreat(Match match, int side)
    {
        if (match.LiveBattle is not { } battle || !battle.CanRetreat(side))
        {
            return false;
        }

        var now = DateTimeOffset.UtcNow;

        if (battle.IsOverAt(now))
        {
            return false;
        }

        match.LiveBattle = battle with
        {
            Retreat = new Retreat(side, Math.Min(battle.ElapsedAt(now), battle.Replay.Duration)),
        };

        return true;
    }

    public static void StartRoundClock(Match match)
        => match.RoundDeadline = DateTimeOffset.UtcNow.AddSeconds(match.RoundSeconds);

    private static void Apply(Match match, RoundOutcome outcome)
    {
        match.History.Add(new RoundResult
        {
            RoundNumber = match.Round,
            Event = match.CurrentEvent,
            Player1 = new RoundSide(match.Player1!.PendingOrder ?? OrderPlan.Empty, outcome.Player1.Kingdom),
            Player2 = new RoundSide(match.Player2!.PendingOrder ?? OrderPlan.Empty, outcome.Player2.Kingdom),
            Events = outcome.Events.ToList(),
            GameOverReason = outcome.GameOverReason,
        });

        match.Player1.Kingdom = outcome.Player1.Kingdom;
        match.Player2.Kingdom = outcome.Player2.Kingdom;
        match.Player1.Caravan = outcome.Player1.Caravan;
        match.Player2.Caravan = outcome.Player2.Caravan;
        match.Player1.PendingOrder = null;
        match.Player2.PendingOrder = null;
        match.GameOverReason = outcome.GameOverReason;

        if (outcome.GameOverReason == GameOverReason.None)
        {
            match.Round += 1;
            match.CurrentEvent = GameEngine.DrawEvent(Random.Shared);
            StartRoundClock(match);
        }
        else
        {
            match.Status = MatchStatus.Completed;
            match.RoundDeadline = null;
        }
    }

    private static void ForfeitIfAbandoned(Match match)
    {
        if (match.GameOverReason != GameOverReason.None)
        {
            return;
        }

        var player1Gone = match.Player1!.MissedRounds >= GameConstants.MissedRoundsBeforeForfeit;
        var player2Gone = match.Player2!.MissedRounds >= GameConstants.MissedRoundsBeforeForfeit;

        if (!player1Gone && !player2Gone)
        {
            return;
        }

        match.GameOverReason = (player1Gone, player2Gone) switch
        {
            (true, true) => GameOverReason.BothDisconnected,
            (true, false) => GameOverReason.Player1Disconnected,
            _ => GameOverReason.Player2Disconnected,
        };

        match.Status = MatchStatus.Completed;
        match.RoundDeadline = null;
    }
}
