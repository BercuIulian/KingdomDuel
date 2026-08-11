namespace KingdomDuel.Application.Features.Matches;

internal static class MatchErrors
{
    public static string AlreadyPlaying(string code) =>
        $"Ai un meci în desfășurare ({code}). Reintră în el sau părăsește-l ca să începi altul.";
}
