using KingdomDuel.Application.Features.Auth;
using KingdomDuel.Domain.Enums;
using KingdomDuel.Shared.Dtos.Matches;
using KingdomDuel.Shared.Responses.Matches;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using FastEndpoints;

namespace KingdomDuel.Application.Features.Matches;

internal sealed class GetMyMatchEndpoint(AppDbContext dbContext)
    : EndpointWithoutRequest<ActiveMatchResponse>
{
    public override void Configure()
    {
        Get("/matches/mine");
        Description(b => b.WithName("GetMyMatch"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.GetUserId();

        var active = await dbContext.Matches
            .Where(m => m.Status == MatchStatus.InProgress
                        && (m.Player1!.UserId == userId || m.Player2!.UserId == userId))
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new ActiveMatchDto
            {
                Code = m.Code,
                Round = m.Round,
                TotalRounds = m.TotalRounds,
            })
            .FirstOrDefaultAsync(ct);

        await Send.OkAsync(new ActiveMatchResponse { Match = active }, ct);
    }
}
