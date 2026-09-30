using KingdomDuel.Application.Features.Auth;
using KingdomDuel.Shared.Dtos.Matches;
using KingdomDuel.Shared.Requests.Matches;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using FastEndpoints;

namespace KingdomDuel.Application.Features.Matches;

internal sealed class GetMatchEndpoint(AppDbContext dbContext, RoundResolver resolver, TimeProvider time)
    : Endpoint<GetMatchRequest, MatchStateDto>
{
    public override void Configure()
    {
        Get("/matches/{code}");
        Description(b => b.WithName("GetMatch"));
    }

    public override async Task HandleAsync(GetMatchRequest req, CancellationToken ct)
    {
        var userId = User.GetUserId();
        var match = await dbContext.Matches.FindByCodeAsync(req.Code, ct);

        if (match is null || match.SideOf(userId) == 0)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var settled = resolver.SettleIfDue(match);

        if (settled || resolver.ExpireIfDue(match))
        {
            MatchRecorder.RecordCompleted(dbContext, match, time.GetUtcNow());

            try
            {
                await dbContext.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                dbContext.ChangeTracker.Clear();
                match = await dbContext.Matches.FindByCodeAsync(req.Code, ct);

                if (match is null)
                {
                    await Send.NotFoundAsync(ct);
                    return;
                }
            }
        }

        await Send.OkAsync(match.ToStateDto(userId, time.GetUtcNow()), ct);
    }
}
