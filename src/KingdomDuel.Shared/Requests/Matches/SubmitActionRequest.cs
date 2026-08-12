using KingdomDuel.Domain.Enums;

namespace KingdomDuel.Shared.Requests.Matches;

public sealed class SubmitActionRequest
{
    public string Code { get; set; } = string.Empty;

    public ActionType Action { get; set; }
}
