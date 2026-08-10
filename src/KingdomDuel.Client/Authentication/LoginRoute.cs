namespace KingdomDuel.Client.Authentication;

public static class LoginRoute
{
    public const string Path = "login";
    public const string ReturnUrlParameter = "returnUrl";

    public const string AfterSignIn = "/";

    public static string WithReturnUrl(string relativePath)
    {
        var target = relativePath.TrimStart('/');

        return string.IsNullOrEmpty(target)
            ? $"/{Path}"
            : $"/{Path}?{ReturnUrlParameter}={Uri.EscapeDataString("/" + target)}";
    }
}
