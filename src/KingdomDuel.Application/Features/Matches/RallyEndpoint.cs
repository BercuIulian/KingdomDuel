using KingdomDuel.Application.Features.Auth;
using KingdomDuel.Domain.Enums;
using KingdomDuel.Shared.Dtos.Matches;
using KingdomDuel.Shared.Requests.Matches;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using FastEndpoints;

namespace KingdomDuel.Application.Features.Matches;

internal sealed class RallyEndpoint(AppDbContext dbContext, RoundResolver resolver, TimeProvider time) : Endpoint<RallyRequest, MatchStateDto>
{
    private const int MaxAttempts = 2;

    public override void Configure()
    {
        Post("/matches/{code}/rally");
        Description(b => b.WithName("Rally"));
    }

    public override async Task HandleAsync(RallyRequest req, CancellationToken ct)
    {
        var userId = User.GetUserId();

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            var match = await dbContext.Matches.FindByCodeAsync(req.Code, ct);
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

            resolver.SettleIfDue(match);

            if (match.BreachCall is not { } call)
            {
                AddError("Nu e nicio spărtură de apărat acum.");
                await Send.ErrorsAsync(cancellation: ct);
                return;
            }

            if (call.Side != side)
            {
                AddError("Numai cetatea spartă își cheamă satul.");
                await Send.ErrorsAsync(cancellation: ct);
                return;
            }

            if (!resolver.Rally(match, side, req.Villagers))
            {
                AddError("A trecut vremea: asaltul a intrat deja în cetate.");
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

            await Send.OkAsync(match.ToStateDto(userId, time.GetUtcNow()), ct);
            return;
        }
    }
}
