using KingdomDuel.Shared.Dtos.Auth;

namespace KingdomDuel.Shared.Responses.Auth;

public sealed class AuthResponse
{
    public required string Token { get; set; }
    public required DateTimeOffset ExpiresAt { get; set; }
    public required UserProfileDto User { get; set; }
}
