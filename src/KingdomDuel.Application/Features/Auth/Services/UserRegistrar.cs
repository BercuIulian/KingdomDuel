using KingdomDuel.Domain.Entities.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KingdomDuel.Application.Features.Auth.Services;

internal sealed class UserRegistrar(AppDbContext dbContext, IPasswordHasher<User> passwordHasher)
{
    public async Task<User?> RegisterAsync(string userName, string? email, string password, CancellationToken ct)
    {
        var normalized = User.Normalize(userName);

        if (await dbContext.Users.AnyAsync(u => u.NormalizedUserName == normalized, ct))
        {
            return null;
        }

        var user = new User
        {
            UserName = userName,
            Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
            PasswordHash = string.Empty,
        };

        user.PasswordHash = passwordHasher.HashPassword(user, password);

        dbContext.Users.Add(user);

        try
        {
            await dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            return null;
        }

        return user;
    }
}
