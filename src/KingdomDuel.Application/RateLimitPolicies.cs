namespace KingdomDuel.Application;

/// <summary>
/// Numele politicilor de rate limiting. Stau aici, nu în Api, pentru că endpoint-urile
/// le cer prin <c>RequireRateLimiting</c>, iar configurarea lor le înregistrează — două
/// locuri care trebuie să folosească exact același șir.
/// </summary>
public static class RateLimitPolicies
{
    public const string CreateMatch = "create-match";
    public const string Login = "login";
    public const string Register = "register";
    public const string ChangePassword = "change-password";
}
