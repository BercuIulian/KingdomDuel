using KingdomDuel.Domain.Entities.Matches;
using Microsoft.EntityFrameworkCore;

namespace KingdomDuel.Application.Features.Matches;

internal static class MatchQueries
{
    public static Task<Match?> FindByCodeAsync(
        this IQueryable<Match> matches,
        string code,
        CancellationToken ct)
        => matches
            .Include(m => m.Player1).ThenInclude(p => p!.Kingdom)
            .Include(m => m.Player1).ThenInclude(p => p!.User)
            .Include(m => m.Player2).ThenInclude(p => p!.Kingdom)
            .Include(m => m.Player2).ThenInclude(p => p!.User)
            .Include(m => m.History)
            .FirstOrDefaultAsync(m => m.Code == code.ToUpper(), ct);
}
