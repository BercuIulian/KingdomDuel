using KingdomDuel.Application.Features.Auth;
using KingdomDuel.Domain;
using KingdomDuel.Domain.Enums;
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

        var engine = new GameEngine();
        engine.InitializeKingdoms(
            match.Player1.Kingdom,
            match.Player2.Kingdom,
            match.StartingGold,
            match.StartingTroops,
            match.StartingMorale);

        match.Round = 1;
        match.Status = MatchStatus.InProgress;
        match.CurrentEvent = engine.PickRandomEvent();
        match.StartedAt = DateTimeOffset.UtcNow;
        RoundResolver.StartRoundClock(match);
        await dbContext.SaveChangesAsync(ct);

        await Send.OkAsync(match.ToStateDto(userId), ct);
    }
}
