using KingdomDuel.Application.Features.Auth;
using KingdomDuel.Domain;
using KingdomDuel.Domain.Entities.Matches;
using KingdomDuel.Domain.Enums;
using KingdomDuel.Domain.Rounds;
using KingdomDuel.Shared.Dtos.Matches;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using FastEndpoints;

namespace KingdomDuel.Application.Features.Matches;

internal sealed class RematchEndpoint(AppDbContext dbContext, TimeProvider time)
    : EndpointWithoutRequest<MatchStateDto>
{
    private const int Attempts = 3;

    public override void Configure()
    {
        Post("/matches/{code}/rematch");
        Description(b => b.WithName("Rematch"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.GetUserId();
        var code = Route<string>("code")!;

        for (var attempt = 1; ; attempt++)
        {
            var match = await dbContext.Matches.FindByCodeAsync(code, ct);

            if (match is null || match.SideOf(userId) == 0)
            {
                await Send.NotFoundAsync(ct);
                return;
            }

            if (match.RematchCode is null)
            {
                var refusal = await AskAsync(match, userId, ct);

                if (refusal is not null)
                {
                    AddError(refusal);
                    await Send.ErrorsAsync(cancellation: ct);
                    return;
                }
            }

            try
            {
                await dbContext.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException) when (attempt < Attempts)
            {
                dbContext.ChangeTracker.Clear();
                continue;
            }

            await Send.OkAsync(match.ToStateDto(userId, time.GetUtcNow()), ct);
            return;
        }
    }

    private async Task<string?> AskAsync(Match match, Guid userId, CancellationToken ct)
    {
        if (match.Status != MatchStatus.Completed)
        {
            return "Revanșa se cere doar după ce meciul s-a încheiat.";
        }

        if (!match.CanRematch())
        {
            return "Adversarul a părăsit masa.";
        }

        if (match.SideOf(userId) == 1)
        {
            match.Player1WantsRematch = true;
        }
        else
        {
            match.Player2WantsRematch = true;
        }

        if (!match.Player1WantsRematch || !match.Player2WantsRematch)
        {
            return null;
        }

        if (await dbContext.Matches.FindActiveCodeAsync(match.Player1!.UserId, ct) is { } busy1)
        {
            return match.Player1.UserId == userId ? MatchErrors.AlreadyPlaying(busy1) : "Adversarul a început deja alt meci.";
        }

        if (await dbContext.Matches.FindActiveCodeAsync(match.Player2!.UserId, ct) is { } busy2)
        {
            return match.Player2.UserId == userId ? MatchErrors.AlreadyPlaying(busy2) : "Adversarul a început deja alt meci.";
        }

        var startsAt = time.GetUtcNow().AddSeconds(GameConstants.RematchCountdownSeconds);
        var rematch = await RematchOfAsync(match, startsAt, ct);

        dbContext.Matches.Add(rematch);
        match.RematchCode = rematch.Code;
        match.RematchStartsAt = startsAt;

        return null;
    }

    private async Task<Match> RematchOfAsync(Match match, DateTimeOffset startsAt, CancellationToken ct)
    {
        string code;
        do
        {
            code = MatchCodeGenerator.Generate();
        } while (await dbContext.Matches.AnyAsync(m => m.Code == code, ct));

        var start = KingdomState.Start(match.StartingGold, match.StartingTroops, match.StartingMorale, match.StartingVillagers);

        return new Match
        {
            Code = code,
            Status = MatchStatus.InProgress,
            TotalRounds = match.TotalRounds,
            StartingGold = match.StartingGold,
            StartingTroops = match.StartingTroops,
            StartingVillagers = match.StartingVillagers,
            StartingMorale = match.StartingMorale,
            RoundSeconds = match.RoundSeconds,
            Player1 = new PlayerSlot { UserId = match.Player1!.UserId, User = match.Player1.User, IsReady = true, Kingdom = start },
            Player2 = new PlayerSlot { UserId = match.Player2!.UserId, User = match.Player2.User, IsReady = true, Kingdom = start },
            StartedAt = startsAt,
            RoundDeadline = startsAt.AddSeconds(match.RoundSeconds),
        };
    }
}
