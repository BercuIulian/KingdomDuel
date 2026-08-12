using Microsoft.EntityFrameworkCore;

namespace KingdomDuel.Application.Features.Auth.Services;

internal sealed class TokenRevoker(AppDbContext dbContext)
{
    public Task RevokeAllAsync(Guid userId, CancellationToken ct)
        => dbContext.Users
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.TokenVersion, u => u.TokenVersion + 1), ct);
}
