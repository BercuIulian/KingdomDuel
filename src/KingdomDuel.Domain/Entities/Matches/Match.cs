using KingdomDuel.Domain.Contracts;
using KingdomDuel.Domain.Enums;

namespace KingdomDuel.Domain.Entities.Matches;

/// <summary>
/// A single match between two players, identified by a short join code.
/// Aggregate root: Player1/Player2 (player_slots), their Kingdom (kingdoms) and
/// History (round_results) are separate related tables. Only PendingActions - a
/// transient map of primitives, not an entity - stays as a jsonb column.
/// </summary>
public sealed class Match : Entity
{
    public required string Code { get; init; }
    public MatchStatus Status { get; set; } = MatchStatus.WaitingForPlayer;
    public int Round { get; set; } = 1;
    public GameEventType CurrentEvent { get; set; } = GameEventType.None;
    public GameOverReason GameOverReason { get; set; } = GameOverReason.None;

    public PlayerSlot? Player1 { get; set; }
    public PlayerSlot? Player2 { get; set; }

    /// <summary>Actions submitted for the round in progress, keyed by PlayerId, cleared once both are in.</summary>
    public Dictionary<string, ActionType> PendingActions { get; private set; } = new();

    public List<RoundResult> History { get; private set; } = new();

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
