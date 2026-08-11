using KingdomDuel.Application.Features.Auth;
using KingdomDuel.Domain.Entities.Matches;
using KingdomDuel.Shared.Responses.Matches;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using FastEndpoints;

namespace KingdomDuel.Application.Features.Matches;

internal sealed class JoinMatchEndpoint(AppDbContext dbContext)
    : EndpointWithoutRequest<JoinMatchResponse>
{
    public override void Configure()
    {
        Post("/matches/{code}/join");
        Description(b => b.WithName("JoinMatch"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.GetUserId();
        var match = await dbContext.Matches.FindByCodeAsync(Route<string>("code")!, ct);

        if (match is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (match.SideOf(userId) != 0)
        {
            await Send.OkAsync(new JoinMatchResponse { State = match.ToStateDto(userId) }, ct);
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

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        match.Player2 = new PlayerSlot { UserId = userId, User = user };
        await dbContext.SaveChangesAsync(ct);

        await Send.OkAsync(new JoinMatchResponse { State = match.ToStateDto(userId) }, ct);
    }
}
