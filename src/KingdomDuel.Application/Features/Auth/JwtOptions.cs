namespace KingdomDuel.Application.Features.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string SigningKey { get; set; } = string.Empty;

    public string Issuer { get; set; } = "KingdomDuel";

    public string Audience { get; set; } = "KingdomDuel";

    public int LifetimeDays { get; set; } = 7;

    public int RenewWhenRemainingDays { get; set; } = 1;
}
