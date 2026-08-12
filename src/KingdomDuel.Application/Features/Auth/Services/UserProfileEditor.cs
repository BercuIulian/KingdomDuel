using KingdomDuel.Domain.Entities.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KingdomDuel.Application.Features.Auth.Services;

internal sealed record ProfileUpdate(User User, bool RequiresSignIn);

internal sealed class UserProfileEditor(AppDbContext dbContext, IPasswordHasher<User> passwordHasher)
{
    public async Task<ProfileUpdate?> UpdateAsync(
        Guid userId,
        string userName,
        string? email,
        CancellationToken ct)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null)
        {
            return null;
        }

        var normalized = User.Normalize(userName);
        var trimmed = userName.Trim();

        var renamed = !string.Equals(user.UserName, trimmed, StringComparison.Ordinal);
        var normalizedChanged = user.NormalizedUserName != normalized;

        if (normalizedChanged
            && await dbContext.Users.AnyAsync(u => u.Id != userId && u.NormalizedUserName == normalized, ct))
        {
            return null;
        }

        user.UserName = trimmed;
        user.Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();

        if (renamed)
        {
            user.TokenVersion++;
        }

        try
        {
            await dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            return null;
        }

        return new ProfileUpdate(user, renamed);
    }

    public async Task<bool> ChangePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        CancellationToken ct)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null)
        {
            return false;
        }

        var verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, currentPassword);

        if (verification == PasswordVerificationResult.Failed)
        {
            return false;
        }

        user.PasswordHash = passwordHasher.HashPassword(user, newPassword);
        user.TokenVersion++;

        await dbContext.SaveChangesAsync(ct);

        return true;
    }
}
