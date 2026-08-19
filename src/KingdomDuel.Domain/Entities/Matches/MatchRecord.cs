using KingdomDuel.Domain.Contracts;
using KingdomDuel.Domain.Enums;

namespace KingdomDuel.Domain.Entities.Matches;

public sealed class MatchRecord : Entity
{
    public required Guid UserId { get; init; }

    public required string OpponentName { get; init; }

    public required MatchOutcome Outcome { get; init; }

    public required GameOverReason Reason { get; init; }

    public required int RoundsPlayed { get; init; }

    public required int TotalRounds { get; init; }

    public required int YourPower { get; init; }

    public required int OpponentPower { get; init; }

    public required DateTimeOffset StartedAt { get; init; }
}
