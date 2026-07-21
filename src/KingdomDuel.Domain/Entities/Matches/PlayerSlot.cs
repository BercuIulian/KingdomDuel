using KingdomDuel.Domain.Contracts;

namespace KingdomDuel.Domain.Entities.Matches;

public sealed class PlayerSlot : Entity
{
    public required string PlayerId { get; init; }
    public required string DisplayName { get; set; }

    public bool IsReady { get; set; }

    public Kingdom Kingdom { get; set; } = new();
}
