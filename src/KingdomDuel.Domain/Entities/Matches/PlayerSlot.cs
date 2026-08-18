using KingdomDuel.Domain.Contracts;
using KingdomDuel.Domain.Entities.Users;

namespace KingdomDuel.Domain.Entities.Matches;

public sealed class PlayerSlot : Entity
{
    public required Guid UserId { get; init; }

    public User User { get; set; } = null!;

    public bool IsReady { get; set; }

    public int MissedRounds { get; set; }

    public Kingdom Kingdom { get; set; } = new();
}
