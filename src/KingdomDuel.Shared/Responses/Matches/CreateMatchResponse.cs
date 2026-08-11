using KingdomDuel.Shared.Dtos.Matches;

namespace KingdomDuel.Shared.Responses.Matches;

public sealed class CreateMatchResponse
{
    public required string MatchCode { get; set; }
    public required MatchStateDto State { get; set; }
}
