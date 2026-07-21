using KingdomDuel.Domain.Entities.Matches;
using KingdomDuel.Shared.Requests.Matches;
using KingdomDuel.Shared.Responses.Matches;
using KingdomDuel.Shared.Validators.Matches;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using FastEndpoints;

namespace KingdomDuel.Application.Features.Matches;

internal sealed class JoinMatchEndpoint(AppDbContext dbContext)
    : Endpoint<JoinMatchRequest, JoinMatchResponse>
{
    public override void Configure()
    {
        Post("/matches/{code}/join");
        AllowAnonymous();
        Validator<JoinMatchValidator>();
        Description(b => b.WithName("JoinMatch"));
    }

    public override async Task HandleAsync(JoinMatchRequest req, CancellationToken ct)
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

        if (match.Player1 is null)
        {
            AddError("Meciul nu are încă un prim jucător.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        if (match.Player2 is not null)
        {
            AddError("Meciul are deja doi jucători.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        var playerId = Guid.NewGuid().ToString("N");
        match.Player2 = new PlayerSlot { PlayerId = playerId, DisplayName = req.DisplayName };
        await dbContext.SaveChangesAsync(ct);

        await Send.OkAsync(new JoinMatchResponse { PlayerId = playerId, State = match.ToStateDto() }, ct);
    }
}
