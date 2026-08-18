using KingdomDuel.Domain;
using KingdomDuel.Domain.Entities.Matches;
using KingdomDuel.Domain.Enums;

namespace KingdomDuel.Application.Features.Matches;

internal static class RoundResolver
{
    public static bool ExpireIfDue(Match match)
    {
        if (match.Status != MatchStatus.InProgress
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

        var player1Missed = !match.PendingActions.ContainsKey(match.Player1.ActionKey());
        var player2Missed = !match.PendingActions.ContainsKey(match.Player2.ActionKey());

        match.PendingActions.TryAdd(match.Player1.ActionKey(), ActionType.Idle);
        match.PendingActions.TryAdd(match.Player2.ActionKey(), ActionType.Idle);

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

    public static bool ResolveIfReady(Match match)
    {
        if (match.Status != MatchStatus.InProgress
            || match.Player1 is null
            || match.Player2 is null)
        {
            return false;
        }

        if (!match.PendingActions.TryGetValue(match.Player1.ActionKey(), out var player1Action)
            || !match.PendingActions.TryGetValue(match.Player2.ActionKey(), out var player2Action))
        {
            return false;
        }

        var engine = new GameEngine();

        var result = engine.ResolveRound(
            match.Round,
            match.TotalRounds,
            match.CurrentEvent,
            match.Player1.Kingdom,
            match.Player2.Kingdom,
            player1Action,
            player2Action);

        match.History.Add(result);
        match.PendingActions.Clear();
        match.GameOverReason = result.GameOverReason;

        if (result.GameOverReason == GameOverReason.None)
        {
            match.Round += 1;
            match.CurrentEvent = engine.PickRandomEvent();
            StartRoundClock(match);
        }
        else
        {
            match.Status = MatchStatus.Completed;
            match.RoundDeadline = null;
        }

        return true;
    }

    public static void StartRoundClock(Match match)
        => match.RoundDeadline = DateTimeOffset.UtcNow.AddSeconds(match.RoundSeconds);
}
