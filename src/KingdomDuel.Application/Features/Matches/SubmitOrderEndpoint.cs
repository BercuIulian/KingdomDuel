using KingdomDuel.Application.Features.Auth;
using KingdomDuel.Domain.Enums;
using KingdomDuel.Domain.Rounds;
using KingdomDuel.Shared.Dtos.Matches;
using KingdomDuel.Shared.Requests.Matches;
using KingdomDuel.Shared.Validators.Matches;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using FastEndpoints;

namespace KingdomDuel.Application.Features.Matches;

internal sealed class SubmitOrderEndpoint(AppDbContext dbContext)
    : Endpoint<SubmitOrderRequest, MatchStateDto>
{
    private const int MaxAttempts = 2;

    public override void Configure()
    {
        Post("/matches/{code}/orders");
        Validator<SubmitOrderValidator>();
        Description(b => b.WithName("SubmitOrder"));
    }

    public override async Task HandleAsync(SubmitOrderRequest req, CancellationToken ct)
    {
        var userId = User.GetUserId();
        var plan = new OrderPlan(req.Troops.ToDictionary(a => a.Direction, a => a.Troops));

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            var match = await dbContext.Matches.FindByCodeAsync(req.Code, ct);
            var slot = match?.SlotOf(userId);

            if (match is null || slot is null)
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

            if (!plan.FitsIn(slot.Kingdom))
            {
                AddError($"Ai trimis {plan.Committed} soldați, dar oastea are {slot.Kingdom.Troops}.");
                await Send.ErrorsAsync(cancellation: ct);
                return;
            }

            if (plan[ActionType.Trade] > 0 && slot.Caravan is { } away)
            {
                AddError($"Caravana e încă pe drum cu {away.Troops} soldați. Se întoarce în runda {away.AvailableInRound}.");
                await Send.ErrorsAsync(cancellation: ct);
                return;
            }

            if (!RoundResolver.ExpireIfDue(match))
            {
                slot.PendingOrder = plan;
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
