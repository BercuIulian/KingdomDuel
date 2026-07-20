using KingdomDuel.Domain.Contracts;

namespace KingdomDuel.Domain.Entities.Matches;

/// <summary>One human player seated in a match. Persisted as its own "player_slots" table.</summary>
public sealed class PlayerSlot : Entity
{
    public required string PlayerId { get; init; }
    public required string DisplayName { get; set; }
    public Kingdom Kingdom { get; set; } = new();
}
