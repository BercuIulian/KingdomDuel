using KingdomDuel.Application.Features.Auth;
using KingdomDuel.Shared.Dtos.Matches;
using KingdomDuel.Shared.Requests.Matches;
using KingdomDuel.Shared.Validators.Matches;
using Microsoft.AspNetCore.Builder;
using FastEndpoints;

namespace KingdomDuel.Application.Features.Matches;

internal sealed class UpdateLobbyEndpoint(AppDbContext dbContext)
    : Endpoint<UpdateLobbyRequest, MatchStateDto>
{
    public override void Configure()
    {
        Put("/matches/{code}/lobby");
        Validator<UpdateLobbyValidator>();
        Description(b => b.WithName("UpdateLobby"));
    }

    public override async Task HandleAsync(UpdateLobbyRequest req, CancellationToken ct)
    {
        var userId = User.GetUserId();
        var match = await dbContext.Matches.FindByCodeAsync(req.Code, ct);
        var slot = match?.SlotOf(userId);

        if (match is null || slot is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (match.Player1?.UserId == userId)
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

        await Send.OkAsync(match.ToStateDto(userId), ct);
    }
}
