using KingdomDuel.Domain.Contracts;
using KingdomDuel.Domain.Entities.Users;
using KingdomDuel.Domain.Rounds;

namespace KingdomDuel.Domain.Entities.Matches;

public sealed class PlayerSlot : Entity
{
    public required Guid UserId { get; init; }

    public User User { get; set; } = null!;

    public bool IsReady { get; set; }

    public int MissedRounds { get; set; }

    public KingdomState Kingdom { get; set; } = KingdomState.Start(
        GameConstants.StartingResource,
        GameConstants.StartingResource,
        GameConstants.StartingResource);

    public OrderPlan? PendingOrder { get; set; }
}
