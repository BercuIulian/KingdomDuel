using KingdomDuel.Application.Features.Auth;
using KingdomDuel.Domain.Enums;
using KingdomDuel.Domain.Rounds;
using KingdomDuel.Shared.Dtos.Matches;
using Microsoft.AspNetCore.Builder;
using FastEndpoints;

namespace KingdomDuel.Application.Features.Matches;

internal sealed class StartMatchEndpoint(AppDbContext dbContext)
    : EndpointWithoutRequest<MatchStateDto>
{
    public override void Configure()
    {
        Post("/matches/{code}/start");
        Description(b => b.WithName("StartMatch"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.GetUserId();
        var match = await dbContext.Matches.FindByCodeAsync(Route<string>("code")!, ct);

        if (match is null || match.SideOf(userId) == 0)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (match.Player1?.UserId != userId)
        {
            AddError("Doar gazda poate porni meciul.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        if (match.Player2 is null)
        {
            AddError("Meciul așteaptă încă un al doilea jucător.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        if (!match.Player2.IsReady)
        {
            AddError("Jucătorul 2 nu este pregătit.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        var start = KingdomState.Start(match.StartingGold, match.StartingTroops, match.StartingMorale, match.StartingVillagers);
        match.Player1.Kingdom = start;
        match.Player2.Kingdom = start;
        match.Player1.PendingOrder = null;
        match.Player2.PendingOrder = null;
        match.Player1.Caravan = null;
        match.Player2.Caravan = null;

        match.Round = 1;
        match.Status = MatchStatus.InProgress;
        match.StartedAt = DateTimeOffset.UtcNow;
        RoundResolver.StartRoundClock(match);
        await dbContext.SaveChangesAsync(ct);

        await Send.OkAsync(match.ToStateDto(userId), ct);
    }
}
