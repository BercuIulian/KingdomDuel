namespace KingdomDuel.Client;

public static class Routes
{
    public const string Landing = "/";
    public const string Play = "/play";
    public const string Lobby = "/lobby";
    public const string Game = "/game";
    public const string Login = $"/{LoginSegment}";
    public const string Profile = "/profile";
    public const string NotFound = "/not-found";

    private const string LoginSegment = "login";

    public const string CodeParameter = "code";

    public const string ReturnUrlParameter = "returnUrl";
    public const string NewAccountParameter = "newAccount";

    public const string AfterSignIn = Play;

    public const string NewAccount = $"{Login}?{NewAccountParameter}=true";

    public static string LobbyFor(string matchCode) => WithCode(Lobby, matchCode);

    public static string GameFor(string matchCode) => WithCode(Game, matchCode);

    public static string ProfileSection(string? section)
        => section is null ? Profile : $"{Profile}/{section}";

    public static bool IsLogin(string relativePath)
        => relativePath.TrimStart('/').StartsWith(LoginSegment, StringComparison.OrdinalIgnoreCase);

    public static string LoginWithReturnUrl(string relativePath)
    {
        var target = relativePath.TrimStart('/');

        return target.Length == 0
            ? Login
            : $"{Login}?{ReturnUrlParameter}={Uri.EscapeDataString("/" + target)}";
    }

    public static string SafeReturnUrl(string? returnUrl)
        => returnUrl is not null
           && returnUrl.StartsWith('/')
           && !returnUrl.StartsWith("//")
           && !returnUrl.StartsWith("/\\")
            ? returnUrl
            : AfterSignIn;

    private static string WithCode(string page, string matchCode)
        => $"{page}?{CodeParameter}={Uri.EscapeDataString(matchCode)}";
}
