using KingdomDuel.Domain;
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
    public override void Configure()
    {
        Post("/matches/{code}/actions");
        AllowAnonymous();
        Description(b => b.WithName("SubmitAction"));
    }

    public override async Task HandleAsync(SubmitActionRequest req, CancellationToken ct)
    {
        var match = await dbContext.Matches
            .Include(m => m.Player1).ThenInclude(p => p!.Kingdom)
            .Include(m => m.Player2).ThenInclude(p => p!.Kingdom)
            .Include(m => m.History)
            .FirstOrDefaultAsync(m => m.Code == req.Code.ToUpper(), ct);

        if (match is null)
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

        if (match.Player1.PlayerId != req.PlayerId && match.Player2.PlayerId != req.PlayerId)
        {
            AddError("Jucător necunoscut pentru acest meci.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        match.PendingActions[req.PlayerId] = req.Action;

        var bothSubmitted = match.PendingActions.ContainsKey(match.Player1.PlayerId)
                            && match.PendingActions.ContainsKey(match.Player2.PlayerId);

        if (bothSubmitted)
        {
            var engine = new GameEngine();
            var player1Action = match.PendingActions[match.Player1.PlayerId];
            var player2Action = match.PendingActions[match.Player2.PlayerId];

            var result = engine.ResolveRound(
                match.Round,
                match.TotalRounds,
                match.CurrentEvent,
                match.Player1.Kingdom,
                match.Player2.Kingdom,
                player1Action,
                player2Action);

            match.History.Add(result);
            match.PendingActions.Clear();
            match.GameOverReason = result.GameOverReason;

            if (result.GameOverReason == GameOverReason.None)
            {
                match.Round += 1;
                match.CurrentEvent = engine.PickRandomEvent();
            }
            else
            {
                match.Status = MatchStatus.Completed;
            }
        }

        await dbContext.SaveChangesAsync(ct);
        await Send.OkAsync(match.ToStateDto(), ct);
    }
}
