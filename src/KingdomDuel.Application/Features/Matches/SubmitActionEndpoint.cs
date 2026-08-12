using KingdomDuel.Application.Features.Auth;
using KingdomDuel.Domain;
using KingdomDuel.Domain.Enums;
using KingdomDuel.Shared.Dtos.Matches;
using KingdomDuel.Shared.Requests.Matches;
using Microsoft.AspNetCore.Builder;
using FastEndpoints;

namespace KingdomDuel.Application.Features.Matches;

internal sealed class SubmitActionEndpoint(AppDbContext dbContext)
    : Endpoint<SubmitActionRequest, MatchStateDto>
{
    public override void Configure()
    {
        Post("/matches/{code}/actions");
        Description(b => b.WithName("SubmitAction"));
    }

    public override async Task HandleAsync(SubmitActionRequest req, CancellationToken ct)
    {
        var userId = User.GetUserId();
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

        match.PendingActions[userId.ToString()] = req.Action;

        var bothSubmitted = match.PendingActions.ContainsKey(match.Player1.ActionKey())
                            && match.PendingActions.ContainsKey(match.Player2.ActionKey());

        if (bothSubmitted)
        {
            var engine = new GameEngine();
            var player1Action = match.PendingActions[match.Player1.ActionKey()];
            var player2Action = match.PendingActions[match.Player2.ActionKey()];

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
        await Send.OkAsync(match.ToStateDto(userId), ct);
    }
}
