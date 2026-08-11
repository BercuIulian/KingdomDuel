using KingdomDuel.Application.Features.Auth;
using KingdomDuel.Domain.Entities.Matches;
using KingdomDuel.Shared.Responses.Matches;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using FastEndpoints;

namespace KingdomDuel.Application.Features.Matches;

internal sealed class CreateMatchEndpoint(AppDbContext dbContext)
    : EndpointWithoutRequest<CreateMatchResponse>
{
    public override void Configure()
    {
        Post("/matches");
        Description(b => b.WithName("CreateMatch"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.GetUserId();
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        string code;
        do
        {
            code = MatchCodeGenerator.Generate();
        } while (await dbContext.Matches.AnyAsync(m => m.Code == code, ct));

        var match = new Match
        {
            Code = code,
            Player1 = new PlayerSlot { UserId = userId, User = user },
        };

        dbContext.Matches.Add(match);
        await dbContext.SaveChangesAsync(ct);

        await Send.OkAsync(
            new CreateMatchResponse { MatchCode = match.Code, State = match.ToStateDto(userId) },
            ct);
    }
}
