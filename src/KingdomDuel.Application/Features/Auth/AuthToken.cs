namespace KingdomDuel.Application.Features.Auth;

internal sealed record AuthToken(string Value, DateTimeOffset ExpiresAt);
