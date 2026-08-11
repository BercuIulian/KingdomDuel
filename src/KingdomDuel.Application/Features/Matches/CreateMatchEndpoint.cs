using KingdomDuel.Application.Features.Auth;
using KingdomDuel.Domain.Entities.Matches;
using KingdomDuel.Domain.Enums;
using KingdomDuel.Shared.Responses.Matches;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using FastEndpoints;

namespace KingdomDuel.Application.Features.Matches;

internal sealed class CreateMatchEndpoint(AppDbContext dbContext)
    : EndpointWithoutRequest<CreateMatchResponse>
{
    public override void Configure()
    {
        Post("/matches");
        Options(x => x.RequireRateLimiting(RateLimitPolicies.CreateMatch));
        Description(b => b.WithName("CreateMatch"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.GetUserId();

        var activeCode = await dbContext.Matches.FindActiveCodeAsync(userId, ct);

        if (activeCode is not null)
        {
            AddError(MatchErrors.AlreadyPlaying(activeCode));
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        var openLobbyCode = await dbContext.Matches.FindOwnOpenLobbyCodeAsync(userId, ct);

        if (openLobbyCode is not null)
        {
            var open = await dbContext.Matches.FindByCodeAsync(openLobbyCode, ct);

            if (open is not null)
            {
                await Send.OkAsync(
                    new CreateMatchResponse { MatchCode = open.Code, State = open.ToStateDto(userId) },
                    ct);
                return;
            }
        }

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
