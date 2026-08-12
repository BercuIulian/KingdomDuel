using KingdomDuel.Shared.Dtos.Matches;

namespace KingdomDuel.Shared.Responses.Matches;

public sealed class JoinMatchResponse
{
    public required MatchStateDto State { get; set; }
}
