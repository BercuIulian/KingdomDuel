using KingdomDuel.Domain.Enums;

namespace KingdomDuel.Shared.Dtos.Matches;

public sealed class SpyRingDto
{
    public SpyTier Tier { get; set; }
    public int Villagers { get; set; }
    public int Upkeep { get; set; }
}
