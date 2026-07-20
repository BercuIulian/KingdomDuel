namespace KingdomDuel.Application.Features.Matches;

internal static class MatchCodeGenerator
{
    // Ambiguous characters (0/O, 1/I) are left out so codes are easy to read aloud and type.
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int Length = 6;

    public static string Generate()
    {
        Span<char> buffer = stackalloc char[Length];
        for (var i = 0; i < buffer.Length; i++)
        {
            buffer[i] = Alphabet[Random.Shared.Next(Alphabet.Length)];
        }

        return new string(buffer);
    }
}
