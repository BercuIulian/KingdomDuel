using KingdomDuel.Shared.Dtos.Matches;

namespace KingdomDuel.Shared.Requests.Matches;

public sealed class SubmitOrderRequest
{
    public string Code { get; set; } = string.Empty;

    public List<OrderAllocationDto> Troops { get; set; } = new();
}
