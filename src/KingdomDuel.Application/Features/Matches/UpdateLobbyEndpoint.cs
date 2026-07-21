using KingdomDuel.Domain.Entities.Matches;
using KingdomDuel.Shared.Dtos.Matches;
using KingdomDuel.Shared.Requests.Matches;
using KingdomDuel.Shared.Validators.Matches;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using FastEndpoints;

namespace KingdomDuel.Application.Features.Matches;

internal sealed class UpdateLobbyEndpoint(AppDbContext dbContext)
    : Endpoint<UpdateLobbyRequest, MatchStateDto>
{
    public override void Configure()
    {
        Put("/matches/{code}/lobby");
        AllowAnonymous();
        Validator<UpdateLobbyValidator>();
        Description(b => b.WithName("UpdateLobby"));
    }

    public override async Task HandleAsync(UpdateLobbyRequest req, CancellationToken ct)
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

        var isHost = match.Player1?.PlayerId == req.PlayerId;

        PlayerSlot? slot =
            isHost ? match.Player1 :
            match.Player2?.PlayerId == req.PlayerId ? match.Player2 :
            null;

        if (slot is null)
        {
            AddError("Jucător necunoscut pentru acest meci.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        slot.DisplayName = req.DisplayName.Trim();

        if (isHost)
        {
            match.TotalRounds = req.TotalRounds;
            match.StartingGold = req.StartingGold;
            match.StartingTroops = req.StartingTroops;
            match.StartingMorale = req.StartingMorale;
        }
        else
        {
            slot.IsReady = req.IsReady;
        }

        await dbContext.SaveChangesAsync(ct);

        await Send.OkAsync(match.ToStateDto(), ct);
    }
}
