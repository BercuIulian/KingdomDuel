namespace KingdomDuel.Shared.Validators.Auth;

public static class AuthRules
{
    public const int UserNameMinLength = 3;
    public const int UserNameMaxLength = 24;

    public const string UserNamePattern = "^[a-zA-Z0-9._-]+$";

    public const string UserNamePatternMessage =
        "Numele de utilizator poate conține doar litere fără diacritice, cifre și semnele - _ .";

    public const int PasswordMinLength = 8;
    public const int PasswordMaxLength = 128;

    public const int EmailMaxLength = 256;
}
