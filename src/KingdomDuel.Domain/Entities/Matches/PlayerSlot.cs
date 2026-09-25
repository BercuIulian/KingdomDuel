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
        GameConstants.StartingGold,
        GameConstants.StartingSoldiers,
        GameConstants.StartingResource,
        GameConstants.StartingVillagers);

    public OrderPlan? PendingOrder { get; set; }

    public Caravan? Caravan { get; set; }

    public SpyRing? Spies { get; set; }

    public SpyReport? Report { get; set; }
}
