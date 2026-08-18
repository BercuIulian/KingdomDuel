using KingdomDuel.Application.Features.Auth;
using KingdomDuel.Domain.Enums;
using KingdomDuel.Shared.Dtos.Matches;
using KingdomDuel.Shared.Requests.Matches;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using FastEndpoints;

namespace KingdomDuel.Application.Features.Matches;

internal sealed class SubmitActionEndpoint(AppDbContext dbContext)
    : Endpoint<SubmitActionRequest, MatchStateDto>
{
    private const int MaxAttempts = 2;

    public override void Configure()
    {
        Post("/matches/{code}/actions");
        Description(b => b.WithName("SubmitAction"));
    }

    public override async Task HandleAsync(SubmitActionRequest req, CancellationToken ct)
    {
        var userId = User.GetUserId();

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            var match = await dbContext.Matches.FindByCodeAsync(req.Code, ct);

            if (match is null || match.SideOf(userId) == 0)
            {
                await Send.NotFoundAsync(ct);
                return;
            }

            if (match.Status != MatchStatus.InProgress)
            {
                AddError("Meciul nu este în desfășurare.");
                await Send.ErrorsAsync(cancellation: ct);
                return;
            }

            if (match.Player1 is null || match.Player2 is null)
            {
                AddError("Meciul așteaptă încă un al doilea jucător.");
                await Send.ErrorsAsync(cancellation: ct);
                return;
            }

            if (!RoundResolver.ExpireIfDue(match))
            {
                match.PendingActions[userId.ToString()] = req.Action;
                RoundResolver.ResolveIfReady(match);
            }

            MatchRecorder.RecordCompleted(dbContext, match);

            try
            {
                await dbContext.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxAttempts)
            {
                dbContext.ChangeTracker.Clear();
                continue;
            }

            await Send.OkAsync(match.ToStateDto(userId), ct);
            return;
        }
    }
}
