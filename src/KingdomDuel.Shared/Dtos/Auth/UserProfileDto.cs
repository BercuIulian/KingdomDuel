namespace KingdomDuel.Shared.Dtos.Auth;

public sealed class UserProfileDto
{
    public required Guid Id { get; set; }
    public required string UserName { get; set; }
    public string? Email { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
}
