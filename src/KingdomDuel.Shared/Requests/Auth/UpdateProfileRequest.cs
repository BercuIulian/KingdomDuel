namespace KingdomDuel.Shared.Requests.Auth;

public sealed class UpdateProfileRequest
{
    public string UserName { get; set; } = string.Empty;
    public string? Email { get; set; }
}
