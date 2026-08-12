using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace KingdomDuel.Application.Features.Auth.Services;

internal sealed class TokenVersionValidator(AppDbContext dbContext)
{
    public async Task<bool> IsCurrentAsync(ClaimsPrincipal principal, CancellationToken ct)
    {
        var userId = principal.FindUserId();
        var version = principal.FindTokenVersion();

        if (userId is null || version is null)
        {
            return false;
        }

        var current = await dbContext.Users
            .Where(u => u.Id == userId)
            .Select(u => (int?)u.TokenVersion)
            .FirstOrDefaultAsync(ct);

        return current == version;
    }
}
