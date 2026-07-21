using KingdomDuel.Shared.Requests.Matches;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using FastEndpoints;

namespace KingdomDuel.Application.Features.Matches;

internal sealed class LeaveMatchEndpoint(AppDbContext dbContext)
    : Endpoint<LeaveMatchRequest>
{
    public override void Configure()
    {
        Post("/matches/{code}/leave");
        AllowAnonymous();
        Description(b => b.WithName("LeaveMatch"));
    }

    public override async Task HandleAsync(LeaveMatchRequest req, CancellationToken ct)
    {
        var match = await dbContext.Matches
            .Include(m => m.Player1).ThenInclude(p => p!.Kingdom)
            .Include(m => m.Player2).ThenInclude(p => p!.Kingdom)
            .FirstOrDefaultAsync(m => m.Code == req.Code.ToUpper(), ct);

        if (match is null)
        {
            await Send.NoContentAsync(ct);
            return;
        }

        if (match.Player1?.PlayerId == req.PlayerId)
        {
            dbContext.Matches.Remove(match);
        }
        else if (match.Player2?.PlayerId == req.PlayerId)
        {
            var slot = match.Player2;
            match.Player2 = null;
            dbContext.Remove(slot);
        }

        await dbContext.SaveChangesAsync(ct);
        await Send.NoContentAsync(ct);
    }
}
