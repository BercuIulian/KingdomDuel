using KingdomDuel.Application.Features.Auth;
using KingdomDuel.Domain;
using KingdomDuel.Domain.Enums;
using KingdomDuel.Domain.Rounds;
using KingdomDuel.Shared.Dtos.Matches;
using KingdomDuel.Shared.Requests.Matches;
using KingdomDuel.Shared.Validators.Matches;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using FastEndpoints;

namespace KingdomDuel.Application.Features.Matches;

internal sealed class SubmitOrderEndpoint(AppDbContext dbContext, RoundResolver resolver, TimeProvider time)
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
        var plan = new OrderPlan(
            req.Troops.ToDictionary(a => a.Direction, a => a.Troops),
            req.Feast,
            req.Purchases,
            req.Hired,
            req.Armed,
            req.Spies,
            req.RecallSpies,
            req.Hunt);

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

            resolver.SettleIfDue(match);

            if (match.LiveBattle is not null)
            {
                AddError("Bătălia e în desfășurare. Ordinul următor se dă după ce se încheie.");
                await Send.ErrorsAsync(cancellation: ct);
                return;
            }

            if (match.MarchChoice is not null)
            {
                AddError("Se hotărăște dacă oastea merge mai departe. Ordinul următor vine după.");
                await Send.ErrorsAsync(cancellation: ct);
                return;
            }

            if (plan.Committed > slot.Kingdom.Troops)
            {
                AddError($"Ai trimis {plan.Committed} soldați, dar oastea are {slot.Kingdom.Troops}.");
                await Send.ErrorsAsync(cancellation: ct);
                return;
            }

            if (plan.Sent > slot.Kingdom.Villagers)
            {
                AddError($"Ai trimis {plan.Sent} săteni la negoț, dar satul are {slot.Kingdom.Villagers}.");
                await Send.ErrorsAsync(cancellation: ct);
                return;
            }

            if (plan.Armed > slot.Kingdom.Villagers)
            {
                AddError($"Vrei să înarmezi {plan.Armed} săteni, dar satul are {slot.Kingdom.Villagers}.");
                await Send.ErrorsAsync(cancellation: ct);
                return;
            }

            if (plan.Spies is { } wanted)
            {
                var leaving = GameConstants.SpyVillagers(wanted) - (slot.Spies?.Villagers ?? 0);

                if (leaving > slot.Kingdom.Villagers)
                {
                    AddError($"Iscoada cere {leaving} săteni în plus, dar satul are {slot.Kingdom.Villagers}.");
                    await Send.ErrorsAsync(cancellation: ct);
                    return;
                }
            }

            if (plan.GoldCost > slot.Kingdom.Gold)
            {
                AddError($"Ordinul costă {plan.GoldCost} aur, dar trezoreria are {slot.Kingdom.Gold}.");
                await Send.ErrorsAsync(cancellation: ct);
                return;
            }

            if (plan.Has(PurchaseType.Fortify) && slot.Kingdom.Fortification >= GameConstants.FortifyMaxLevel)
            {
                AddError($"Zidul e fortificat la maximum ({GameConstants.FortifyMaxLevel}).");
                await Send.ErrorsAsync(cancellation: ct);
                return;
            }

            if (plan.Has(PurchaseType.Fortify) && slot.Kingdom.WallIsDown)
            {
                AddError("Zidul e dărâmat: întâi se ridică la loc, abia apoi se întărește.");
                await Send.ErrorsAsync(cancellation: ct);
                return;
            }

            if (plan.Has(PurchaseType.Repair) && !slot.Kingdom.WallIsDown)
            {
                AddError("Zidul e în picioare, n-ai ce repara.");
                await Send.ErrorsAsync(cancellation: ct);
                return;
            }

            var back = plan.Has(PurchaseType.RepairToTwo) ? 2 : plan.Has(PurchaseType.RepairToOne) ? 1 : 0;

            if (back > 0 && (!plan.Has(PurchaseType.Repair) || slot.Kingdom.RuinedFrom < back))
            {
                AddError("Zidul se ridică la loc doar până la nivelul de dinainte, și numai odată cu repararea.");
                await Send.ErrorsAsync(cancellation: ct);
                return;
            }

            if (plan.Has(PurchaseType.Catapult) && slot.Kingdom.Catapults >= GameConstants.CatapultMaxLevel)
            {
                AddError($"Ai deja {GameConstants.CatapultMaxLevel} catapulte, mai multe nu încap.");
                await Send.ErrorsAsync(cancellation: ct);
                return;
            }

            if (plan[ActionType.Trade] > 0 && slot.Caravan is { } away)
            {
                AddError($"Caravana e încă pe drum cu {away.Troops} soldați. Se întoarce în runda {away.AvailableInRound}.");
                await Send.ErrorsAsync(cancellation: ct);
                return;
            }

            if (!resolver.ExpireIfDue(match))
            {
                slot.PendingOrder = plan;
                resolver.ResolveIfReady(match);
            }

            MatchRecorder.RecordCompleted(dbContext, match, time.GetUtcNow());

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
