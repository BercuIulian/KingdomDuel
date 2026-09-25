using KingdomDuel.Domain.Enums;
using KingdomDuel.Domain.Rounds;

namespace KingdomDuel.Shared.Dtos.Matches;

public sealed class RoundRecapDto
{
    public int Round { get; set; }
    public int GoldDelta { get; set; }
    public int TroopsDelta { get; set; }
    public int VillagersDelta { get; set; }
    public int MoraleDelta { get; set; }
    public List<RoundLineDto> Lines { get; set; } = [];
}

public sealed class RoundLineDto
{
    public RoundEventKind Kind { get; set; }
    public int Amount { get; set; }
    public ActionType? Direction { get; set; }
    public PurchaseType? Purchase { get; set; }
}
