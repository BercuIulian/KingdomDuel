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
            || match.MarchChoice is not null
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
            || match.MarchChoice is not null
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
        var now = DateTimeOffset.UtcNow;
        var changed = false;

        if (match.LiveBattle is { } battle && battle.IsOverAt(now))
        {
            match.LiveBattle = null;

            var afterBattle = GameEngine.SettleBattle(
                battle.BeforeBattle,
                battle.Replay,
                battle.EndsAt,
                match.Round,
                match.TotalRounds,
                battle.Retreat);

            var chooser = GameEngine.MarchChoiceSide(battle.Replay, battle.EndsAt, battle.Retreat);

            if (chooser == 0)
            {
                Apply(match, afterBattle);
            }
            else
            {
                match.MarchChoice = new MarchChoice(
                    chooser,
                    GameEngine.SurvivingExpedition(battle.Replay, battle.EndsAt, chooser),
                    now.AddSeconds(GameConstants.MarchChoiceSeconds),
                    afterBattle with { GameOverReason = GameOverReason.None });
            }

            changed = true;
        }

        if (match.MarchChoice is { } choice && choice.IsDueAt(DateTimeOffset.UtcNow))
        {
            TurnBack(match, choice);
            changed = true;
        }

        return changed;
    }

    public static bool March(Match match, int side, bool advance)
    {
        if (match.MarchChoice is not { } choice || choice.Side != side || choice.IsDueAt(DateTimeOffset.UtcNow))
        {
            return false;
        }

        if (!advance)
        {
            TurnBack(match, choice);

            return true;
        }

        var defenderSide = side == 1 ? 2 : 1;
        var defender = defenderSide == 1 ? choice.AfterField.Player1 : choice.AfterField.Player2;
        var defenceOrder = (defenderSide == 1 ? match.Player1 : match.Player2)!.PendingOrder ?? OrderPlan.Empty;

        var events = choice.AfterField.Events.ToList();
        events.Add(new RoundEvent(RoundEventKind.MarchedOn, side, choice.Army, ActionType.Attack));

        var attacker = side == 1 ? choice.AfterField.Player1 : choice.AfterField.Player2;
        var replay = GameEngine.PlanAssault(
            side,
            choice.Army,
            attacker.Kingdom.Catapults,
            defender,
            defenceOrder,
            Random.Shared);

        match.MarchChoice = null;
        match.LiveBattle = new LiveBattle(
            replay,
            choice.AfterField with { Events = events },
            DateTimeOffset.UtcNow);

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

    private static void TurnBack(Match match, MarchChoice choice)
    {
        var events = choice.AfterField.Events.ToList();
        events.Add(new RoundEvent(RoundEventKind.TurnedBack, choice.Side, choice.Army, ActionType.Attack));

        match.MarchChoice = null;
        Apply(match, choice.AfterField with
        {
            Events = events,
            GameOverReason = GameEngine.EvaluateGameOver(
                choice.AfterField.Player1,
                choice.AfterField.Player2,
                match.Round,
                match.TotalRounds),
        });
    }

    public static void StartRoundClock(Match match)
        => match.RoundDeadline = DateTimeOffset.UtcNow.AddSeconds(match.RoundSeconds);

    private static void Apply(Match match, RoundOutcome outcome)
    {
        match.History.Add(new RoundResult
        {
            RoundNumber = match.Round,
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
