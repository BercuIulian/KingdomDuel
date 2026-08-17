using KingdomDuel.Domain;
using KingdomDuel.Domain.Entities.Matches;
using KingdomDuel.Domain.Enums;

namespace KingdomDuel.Application.Features.Matches;

internal static class RoundResolver
{
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
        }
        else
        {
            match.Status = MatchStatus.Completed;
        }

        return true;
    }
}
