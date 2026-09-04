using KingdomDuel.Domain.Enums;

namespace KingdomDuel.Domain;

/// <summary>
/// Ordinul trimis de un jucător pentru runda curentă. Atacul poartă cu el câte
/// trupe pleacă în expediție; celelalte ordine au <see cref="Troops"/> zero.
/// Numărul e o declarație — motorul îl plafonează la ce are regatul și la cât
/// permite evenimentul rundei.
/// </summary>
public sealed record PendingOrder(ActionType Action, int Troops)
{
    public static PendingOrder Of(ActionType action) => new(action, 0);
}
