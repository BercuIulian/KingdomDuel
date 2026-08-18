using KingdomDuel.Domain;
using KingdomDuel.Domain.Entities.Matches;
using KingdomDuel.Domain.Enums;

namespace KingdomDuel.Application.Features.Matches;

internal static class MatchRecorder
{
    public static void RecordCompleted(AppDbContext dbContext, Match match)
    {
        if (match.Status != MatchStatus.Completed
            || match.Player1 is null
            || match.Player2 is null)
        {
            return;
        }

        var startedAt = match.StartedAt ?? DateTimeOffset.UtcNow;

        dbContext.MatchRecords.Add(EntryFor(match, side: 1, startedAt));
        dbContext.MatchRecords.Add(EntryFor(match, side: 2, startedAt));
    }

    private static MatchRecord EntryFor(Match match, int side, DateTimeOffset startedAt)
    {
        var you = side == 1 ? match.Player1! : match.Player2!;
        var opponent = side == 1 ? match.Player2! : match.Player1!;

        return new MatchRecord
        {
            UserId = you.UserId,
            OpponentName = opponent.User.UserName,
            Outcome = GameOutcome.OutcomeFor(match.GameOverReason, side),
            Reason = match.GameOverReason,
            RoundsPlayed = match.Round,
            TotalRounds = match.TotalRounds,
            YourPower = you.Kingdom.TotalPower,
            OpponentPower = opponent.Kingdom.TotalPower,
            StartedAt = startedAt,
        };
    }
}
