using KingdomDuel.Domain.Enums;
using KingdomDuel.Shared.Dtos.Matches;

namespace KingdomDuel.Shared.Requests.Matches;

public sealed class SubmitOrderRequest
{
    public string Code { get; set; } = string.Empty;

    public List<OrderAllocationDto> Troops { get; set; } = new();

    public FeastTier? Feast { get; set; }

    public int Hired { get; set; }

    public int Armed { get; set; }

    public List<PurchaseType> Purchases { get; set; } = new();

    public SpyTier? Spies { get; set; }

    public bool RecallSpies { get; set; }

    public HuntTier? Hunt { get; set; }
}
