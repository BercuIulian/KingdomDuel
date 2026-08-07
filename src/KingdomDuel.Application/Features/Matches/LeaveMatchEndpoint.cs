using KingdomDuel.Domain.Entities.Matches;
using KingdomDuel.Domain.Enums;
using KingdomDuel.Shared.Requests.Matches;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using FastEndpoints;

namespace KingdomDuel.Application.Features.Matches;

internal sealed class LeaveMatchEndpoint(AppDbContext dbContext)
    : Endpoint<LeaveMatchRequest>
{
    public override void Configure()
    {
        Post("/matches/{code}/leave");
        AllowAnonymous();
        Description(b => b.WithName("LeaveMatch"));
    }

    public override async Task HandleAsync(LeaveMatchRequest req, CancellationToken ct)
    {
        var match = await dbContext.Matches
            .Include(m => m.Player1).ThenInclude(p => p!.Kingdom)
            .Include(m => m.Player2).ThenInclude(p => p!.Kingdom)
            .FirstOrDefaultAsync(m => m.Code == req.Code.ToUpper(), ct);

        if (match is null)
        {
            await Send.NoContentAsync(ct);
            return;
        }

        var isPlayer1 = match.Player1?.PlayerId == req.PlayerId;
        var isPlayer2 = match.Player2?.PlayerId == req.PlayerId;

        if (!isPlayer1 && !isPlayer2)
        {
            await Send.NoContentAsync(ct);
            return;
        }

        switch (match.Status)
        {
            case MatchStatus.InProgress when match.Player1 is not null && match.Player2 is not null:
                match.Status = MatchStatus.Completed;
                match.GameOverReason = isPlayer1 ? GameOverReason.Player1Left : GameOverReason.Player2Left;
                break;

            case MatchStatus.Completed when IsAbandon(match.GameOverReason):
                var abandonedByCaller =
                    (match.GameOverReason == GameOverReason.Player1Left && isPlayer1)
                    || (match.GameOverReason == GameOverReason.Player2Left && isPlayer2);

                if (!abandonedByCaller)
                {
                    RemoveMatch(match);
                }

                break;

            case MatchStatus.Completed:
                ReleaseSlot(match, req.PlayerId);

                if (match.Player1 is null && match.Player2 is null)
                {
                    RemoveMatch(match);
                }

                break;

            default:
                if (isPlayer1)
                {
                    RemoveMatch(match);
                }
                else
                {
                    ReleaseSlot(match, req.PlayerId);
                }

                break;
        }

        await dbContext.SaveChangesAsync(ct);
        await Send.NoContentAsync(ct);
    }

    private static bool IsAbandon(GameOverReason reason) =>
        reason is GameOverReason.Player1Left or GameOverReason.Player2Left;

    private void RemoveMatch(Match match)
    {
        if (match.Player1 is not null)
        {
            dbContext.Remove(match.Player1);
        }

        if (match.Player2 is not null)
        {
            dbContext.Remove(match.Player2);
        }

        dbContext.Matches.Remove(match);
    }

    private void ReleaseSlot(Match match, string playerId)
    {
        if (match.Player1?.PlayerId == playerId)
        {
            var slot = match.Player1;
            match.Player1 = null;
            dbContext.Remove(slot);
        }
        else if (match.Player2?.PlayerId == playerId)
        {
            var slot = match.Player2;
            match.Player2 = null;
            dbContext.Remove(slot);
        }
    }
}
