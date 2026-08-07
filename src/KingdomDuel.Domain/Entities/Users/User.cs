using KingdomDuel.Domain.Contracts;

namespace KingdomDuel.Domain.Entities.Users;

public sealed class User : Entity
{
    private string userName = null!;

    public required string UserName
    {
        get => userName;
        set
        {
            userName = value.Trim();
            NormalizedUserName = Normalize(userName);
        }
    }

    public string NormalizedUserName { get; private set; } = null!;

    public required string PasswordHash { get; set; }

    public required string DisplayName { get; set; }

    public string? Email { get; set; }

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    public static string Normalize(string userName) => userName.Trim().ToUpperInvariant();
}
