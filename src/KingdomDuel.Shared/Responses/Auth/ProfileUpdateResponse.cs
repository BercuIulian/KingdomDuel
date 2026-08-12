using KingdomDuel.Shared.Dtos.Auth;

namespace KingdomDuel.Shared.Responses.Auth;

public sealed class ProfileUpdateResponse
{
    public required UserProfileDto User { get; set; }
    public required bool RequiresSignIn { get; set; }
}
