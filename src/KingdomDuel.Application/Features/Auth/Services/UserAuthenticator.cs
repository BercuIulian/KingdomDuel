using KingdomDuel.Domain.Entities.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KingdomDuel.Application.Features.Auth.Services;

internal sealed class UserAuthenticator(AppDbContext dbContext, IPasswordHasher<User> passwordHasher)
{
    private static readonly User Decoy = new()
    {
        UserName = "decoy",
        PasswordHash = string.Empty,
    };

    private static readonly string DecoyPasswordHash =
        new PasswordHasher<User>().HashPassword(Decoy, Guid.NewGuid().ToString());

    public async Task<User?> AuthenticateAsync(string userName, string password, CancellationToken ct)
    {
        var normalized = User.Normalize(userName);

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.NormalizedUserName == normalized, ct);

        var result = passwordHasher.VerifyHashedPassword(
            user ?? Decoy,
            user?.PasswordHash ?? DecoyPasswordHash,
            password);

        if (user is null || result == PasswordVerificationResult.Failed)
        {
            return null;
        }

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = passwordHasher.HashPassword(user, password);
            await dbContext.SaveChangesAsync(ct);
        }

        return user;
    }
}
