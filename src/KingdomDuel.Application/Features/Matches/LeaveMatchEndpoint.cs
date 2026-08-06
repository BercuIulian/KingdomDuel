using KingdomDuel.Domain.Entities.Matches;
using KingdomDuel.Domain.Enums;
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

        var isPlayer1 = match.Player1?.PlayerId == req.PlayerId;
        var isPlayer2 = match.Player2?.PlayerId == req.PlayerId;

        if (!isPlayer1 && !isPlayer2)
        {
            await Send.NoContentAsync(ct);
            return;
        }

        switch (match.Status)
        {
            // Plecarea din timpul meciului îl încheie pe loc: cel rămas câștigă
            // prin abandon și primește același ecran de final. Ambele sloturi
            // rămân pe poziții, ca ecranul lui să aibă și numele, și regatele.
            case MatchStatus.InProgress when match.Player1 is not null && match.Player2 is not null:
                match.Status = MatchStatus.Completed;
                match.GameOverReason = isPlayer1 ? GameOverReason.Player1Left : GameOverReason.Player2Left;
                break;

            // Meci încheiat prin abandon: cel plecat nu se mai întoarce, deci
            // ieșirea celui rămas închide meciul. Un al doilea apel al celui
            // care a abandonat nu are voie să șteargă nimic — ar putea prinde
            // adversarul înainte să apuce să vadă rezultatul.
            case MatchStatus.Completed when IsAbandon(match.GameOverReason):
                var abandonedByCaller =
                    (match.GameOverReason == GameOverReason.Player1Left && isPlayer1)
                    || (match.GameOverReason == GameOverReason.Player2Left && isPlayer2);

                if (!abandonedByCaller)
                {
                    RemoveMatch(match);
                }

                break;

            // Meci dus până la capăt: fiecare pleacă pe cont propriu, iar meciul
            // dispare abia când au ieșit amândoi, ca rezultatul să rămână
            // vizibil pentru cel care mai e pe pagină.
            case MatchStatus.Completed:
                ReleaseSlot(match, req.PlayerId);

                if (match.Player1 is null && match.Player2 is null)
                {
                    RemoveMatch(match);
                }

                break;

            // Încă în lobby: plecarea gazdei închide meciul pentru toți.
            default:
                if (isPlayer1)
                {
                    RemoveMatch(match);
                }
                else
                {
                    ReleaseSlot(match, req.PlayerId);
                }

                break;
        }

        await dbContext.SaveChangesAsync(ct);
        await Send.NoContentAsync(ct);
    }

    private static bool IsAbandon(GameOverReason reason) =>
        reason is GameOverReason.Player1Left or GameOverReason.Player2Left;

    /// <summary>
    /// Șterge meciul împreună cu sloturile lui. Sloturile trebuie scoase explicit:
    /// cheia străină stă pe „matches” (Player1Id/Player2Id), deci în cascadă merge
    /// invers — ștergerea unui slot ar șterge meciul, nu și viceversa. Rundele și
    /// regatele pleacă singure, fiind dependente de meci, respectiv de slot.
    /// </summary>
    private void RemoveMatch(Match match)
    {
        if (match.Player1 is not null)
        {
            dbContext.Remove(match.Player1);
        }

        if (match.Player2 is not null)
        {
            dbContext.Remove(match.Player2);
        }

        dbContext.Matches.Remove(match);
    }

    private void ReleaseSlot(Match match, string playerId)
    {
        if (match.Player1?.PlayerId == playerId)
        {
            var slot = match.Player1;
            match.Player1 = null;
            dbContext.Remove(slot);
        }
        else if (match.Player2?.PlayerId == playerId)
        {
            var slot = match.Player2;
            match.Player2 = null;
            dbContext.Remove(slot);
        }
    }
}
