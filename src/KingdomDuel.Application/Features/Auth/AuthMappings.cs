using KingdomDuel.Domain.Entities.Users;
using KingdomDuel.Shared.Dtos.Auth;
using KingdomDuel.Shared.Responses.Auth;

namespace KingdomDuel.Application.Features.Auth;

internal static class AuthMappings
{
    public static UserProfileDto ToProfileDto(this User user) => new()
    {
        Id = user.Id,
        UserName = user.UserName,
        Email = user.Email,
        CreatedAt = user.CreatedAt,
    };

    public static AuthResponse ToAuthResponse(this User user, AuthToken token) => new()
    {
        Token = token.Value,
        ExpiresAt = token.ExpiresAt,
        User = user.ToProfileDto(),
    };
}
