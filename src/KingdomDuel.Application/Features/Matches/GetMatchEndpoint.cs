using KingdomDuel.Shared.Dtos.Matches;
using KingdomDuel.Shared.Requests.Matches;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using FastEndpoints;

namespace KingdomDuel.Application.Features.Matches;

internal sealed class GetMatchEndpoint(AppDbContext dbContext)
    : Endpoint<GetMatchRequest, MatchStateDto>
{
    public override void Configure()
    {
        Get("/matches/{code}");
        AllowAnonymous();
        Description(b => b.WithName("GetMatch"));
    }

    public override async Task HandleAsync(GetMatchRequest req, CancellationToken ct)
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

        await Send.OkAsync(match.ToStateDto(), ct);
    }
}
