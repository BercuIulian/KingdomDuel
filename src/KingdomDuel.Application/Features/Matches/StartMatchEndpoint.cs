using KingdomDuel.Domain;
using KingdomDuel.Domain.Enums;
using KingdomDuel.Shared.Dtos.Matches;
using KingdomDuel.Shared.Requests.Matches;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using FastEndpoints;

namespace KingdomDuel.Application.Features.Matches;

internal sealed class StartMatchEndpoint(AppDbContext dbContext)
    : Endpoint<StartMatchRequest, MatchStateDto>
{
    public override void Configure()
    {
        Post("/matches/{code}/start");
        AllowAnonymous();
        Description(b => b.WithName("StartMatch"));
    }

    public override async Task HandleAsync(StartMatchRequest req, CancellationToken ct)
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

        if (match.Player1?.PlayerId != req.PlayerId)
        {
            AddError("Doar gazda poate porni meciul.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        if (match.Player2 is null)
        {
            AddError("Meciul așteaptă încă un al doilea jucător.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        if (!match.Player2.IsReady)
        {
            AddError("Jucătorul 2 nu este pregătit.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        match.Status = MatchStatus.InProgress;
        match.CurrentEvent = new GameEngine().PickRandomEvent();
        await dbContext.SaveChangesAsync(ct);

        await Send.OkAsync(match.ToStateDto(), ct);
    }
}
