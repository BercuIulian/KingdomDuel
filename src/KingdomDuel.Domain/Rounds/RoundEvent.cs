using KingdomDuel.Domain.Enums;

namespace KingdomDuel.Domain.Rounds;

public enum RoundEventKind
{
    OrderGiven,
    CaravanSent,
    CaravanReturned,
    TradeIncome,
    MoraleDecay,
    Feast
}

public sealed record RoundEvent(RoundEventKind Kind, int Side, int Amount = 0, ActionType? Direction = null);
