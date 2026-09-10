using KingdomDuel.Application.Features.Auth;
using KingdomDuel.Domain.Enums;
using KingdomDuel.Shared.Dtos.Matches;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using FastEndpoints;

namespace KingdomDuel.Application.Features.Matches;

internal sealed class RetreatEndpoint(AppDbContext dbContext) : EndpointWithoutRequest<MatchStateDto>
{
    private const int MaxAttempts = 2;

    public override void Configure()
    {
        Post("/matches/{code}/retreat");
        Description(b => b.WithName("Retreat"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.GetUserId();
        var code = Route<string>("code")!;

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            var match = await dbContext.Matches.FindByCodeAsync(code, ct);
            var side = match?.SideOf(userId) ?? 0;

            if (match is null || side == 0)
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

            RoundResolver.SettleIfDue(match);

            if (match.LiveBattle is not { } battle)
            {
                AddError("Nu e nicio bătălie în desfășurare.");
                await Send.ErrorsAsync(cancellation: ct);
                return;
            }

            if (battle.Retreat is not null)
            {
                AddError("Semnalul de retragere a fost deja dat.");
                await Send.ErrorsAsync(cancellation: ct);
                return;
            }

            if (!battle.CanRetreat(side))
            {
                AddError("Cetatea atacată nu se poate retrage — n-are unde.");
                await Send.ErrorsAsync(cancellation: ct);
                return;
            }

            if (!RoundResolver.SignalRetreat(match, side))
            {
                AddError("Bătălia s-a încheiat deja.");
                await Send.ErrorsAsync(cancellation: ct);
                return;
            }

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
