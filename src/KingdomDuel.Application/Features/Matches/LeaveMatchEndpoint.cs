using KingdomDuel.Application.Features.Auth;
using KingdomDuel.Domain.Entities.Matches;
using KingdomDuel.Domain.Enums;
using Microsoft.AspNetCore.Builder;
using FastEndpoints;

namespace KingdomDuel.Application.Features.Matches;

internal sealed class LeaveMatchEndpoint(AppDbContext dbContext)
    : EndpointWithoutRequest
{
    public override void Configure()
    {
        Post("/matches/{code}/leave");
        Description(b => b.WithName("LeaveMatch"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.GetUserId();
        var match = await dbContext.Matches.FindByCodeAsync(Route<string>("code")!, ct);

        if (match is null)
        {
            await Send.NoContentAsync(ct);
            return;
        }

        var side = match.SideOf(userId);

        if (side == 0)
        {
            await Send.NoContentAsync(ct);
            return;
        }

        var isPlayer1 = side == 1;

        switch (match.Status)
        {
            case MatchStatus.InProgress when match.Player1 is not null && match.Player2 is not null:
                match.Status = MatchStatus.Completed;
                match.GameOverReason = isPlayer1 ? GameOverReason.Player1Left : GameOverReason.Player2Left;
                break;

            case MatchStatus.Completed when IsAbandon(match.GameOverReason):
                var abandonedByCaller =
                    (match.GameOverReason == GameOverReason.Player1Left && isPlayer1)
                    || (match.GameOverReason == GameOverReason.Player2Left && !isPlayer1);

                if (!abandonedByCaller)
                {
                    RemoveMatch(match);
                }

                break;

            case MatchStatus.Completed:
                ReleaseSlot(match, userId);

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
                    ReleaseSlot(match, userId);
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

    private void ReleaseSlot(Match match, Guid userId)
    {
        if (match.Player1?.UserId == userId)
        {
            var slot = match.Player1;
            match.Player1 = null;
            dbContext.Remove(slot);
        }
        else if (match.Player2?.UserId == userId)
        {
            var slot = match.Player2;
            match.Player2 = null;
            dbContext.Remove(slot);
        }
    }
}
