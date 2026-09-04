using KingdomDuel.Domain.Enums;

namespace KingdomDuel.Shared.Requests.Matches;

public sealed class SubmitActionRequest
{
    public string Code { get; set; } = string.Empty;

    public ActionType Action { get; set; }

    /// <summary>
    /// Câte trupe pleacă în expediție. Contează doar pentru Atac. E o declarație:
    /// motorul o taie la ce are regatul în clipa rezolvării și la plafonul rundei.
    /// </summary>
    public int Troops { get; set; }
}
