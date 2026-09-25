using KingdomDuel.Application.Features.Auth;
using KingdomDuel.Domain.Enums;
using KingdomDuel.Shared.Dtos.Matches;
using KingdomDuel.Shared.Requests.Matches;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using FastEndpoints;

namespace KingdomDuel.Application.Features.Matches;

internal sealed class MarchEndpoint(AppDbContext dbContext) : Endpoint<MarchRequest, MatchStateDto>
{
    private const int MaxAttempts = 2;

    public override void Configure()
    {
        Post("/matches/{code}/march");
        Description(b => b.WithName("March"));
    }

    public override async Task HandleAsync(MarchRequest req, CancellationToken ct)
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

            RoundResolver.SettleIfDue(match);

            if (match.MarchChoice is not { } choice)
            {
                AddError("Nu e nimic de hotărât acum.");
                await Send.ErrorsAsync(cancellation: ct);
                return;
            }

            if (choice.Side != side)
            {
                AddError("Alegerea e a celui rămas în picioare pe câmp.");
                await Send.ErrorsAsync(cancellation: ct);
                return;
            }

            if (!RoundResolver.March(match, side, req.Advance))
            {
                AddError("Vremea alegerii a trecut, oastea s-a întors acasă.");
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
