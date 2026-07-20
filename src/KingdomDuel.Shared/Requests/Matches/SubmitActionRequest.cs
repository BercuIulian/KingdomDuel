using KingdomDuel.Domain.Enums;

namespace KingdomDuel.Shared.Requests.Matches;

public sealed class SubmitActionRequest
{
    /// <summary>Bound from the route.</summary>
    public string Code { get; set; } = string.Empty;

    public string PlayerId { get; set; } = string.Empty;

    public ActionType Action { get; set; }
}
