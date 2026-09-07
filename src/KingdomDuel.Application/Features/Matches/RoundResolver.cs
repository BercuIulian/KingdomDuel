using KingdomDuel.Domain;
using KingdomDuel.Domain.Entities.Matches;
using KingdomDuel.Domain.Enums;

namespace KingdomDuel.Application.Features.Matches;

internal static class RoundResolver
{
    public static void StartRoundClock(Match match)
        => match.RoundDeadline = DateTimeOffset.UtcNow.AddSeconds(match.RoundSeconds);

    public static void ForfeitIfAbandoned(Match match)
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
