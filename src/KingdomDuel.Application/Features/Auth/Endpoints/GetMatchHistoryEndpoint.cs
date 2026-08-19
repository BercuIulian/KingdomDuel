using FastEndpoints;
using KingdomDuel.Domain.Enums;
using KingdomDuel.Shared.Dtos.Matches;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;

namespace KingdomDuel.Application.Features.Auth.Endpoints;

internal sealed class GetMatchHistoryEndpoint(AppDbContext dbContext)
    : EndpointWithoutRequest<MatchHistoryDto>
{
    private const int EntryCount = 10;

    public override void Configure()
    {
        Get("/auth/profile/history");
        Description(b => b.WithName("GetMatchHistory"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.GetUserId();

        var mine = dbContext.MatchRecords.Where(r => r.UserId == userId);

        var totals = await mine
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Played = g.Count(),
                Won = g.Count(r => r.Outcome == MatchOutcome.Won),
                Lost = g.Count(r => r.Outcome == MatchOutcome.Lost),
                Draw = g.Count(r => r.Outcome == MatchOutcome.Draw),
            })
            .FirstOrDefaultAsync(ct);

        var entries = await mine
            .OrderByDescending(r => r.StartedAt)
            .Take(EntryCount)
            .Select(r => new MatchHistoryEntryDto
            {
                OpponentName = r.OpponentName,
                Outcome = r.Outcome,
                Reason = r.Reason,
                RoundsPlayed = r.RoundsPlayed,
                TotalRounds = r.TotalRounds,
                YourPower = r.YourPower,
                OpponentPower = r.OpponentPower,
                StartedAt = r.StartedAt,
            })
            .ToListAsync(ct);

        await Send.OkAsync(
            new MatchHistoryDto
            {
                Played = totals?.Played ?? 0,
                Won = totals?.Won ?? 0,
                Lost = totals?.Lost ?? 0,
                Draw = totals?.Draw ?? 0,
                Entries = entries,
            },
            ct);
    }
}
