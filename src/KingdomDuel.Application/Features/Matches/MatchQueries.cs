using KingdomDuel.Domain.Entities.Matches;
using KingdomDuel.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace KingdomDuel.Application.Features.Matches;

internal static class MatchQueries
{
    public static Task<string?> FindActiveCodeAsync(
        this IQueryable<Match> matches,
        Guid userId,
        CancellationToken ct)
        => matches
            .Where(m => m.Status == MatchStatus.InProgress
                        && (m.Player1!.UserId == userId || m.Player2!.UserId == userId))
            .Select(m => m.Code)
            .FirstOrDefaultAsync(ct);

    public static Task<string?> FindOwnOpenLobbyCodeAsync(
        this IQueryable<Match> matches,
        Guid userId,
        CancellationToken ct)
        => matches
            .Where(m => m.Status == MatchStatus.WaitingForPlayer && m.Player1!.UserId == userId)
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => m.Code)
            .FirstOrDefaultAsync(ct);

    public static Task<Match?> FindByCodeAsync(
        this IQueryable<Match> matches,
        string code,
        CancellationToken ct)
        => matches
            .Include(m => m.Player1).ThenInclude(p => p!.User)
            .Include(m => m.Player2).ThenInclude(p => p!.User)
            .Include(m => m.History)
            .FirstOrDefaultAsync(m => m.Code == code.ToUpper(), ct);
}
