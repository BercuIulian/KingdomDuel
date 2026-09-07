using KingdomDuel.Domain.Enums;

namespace KingdomDuel.Shared.Dtos.Matches;

public sealed class OrderAllocationDto
{
    public ActionType Direction { get; set; }
    public int Troops { get; set; }
}
