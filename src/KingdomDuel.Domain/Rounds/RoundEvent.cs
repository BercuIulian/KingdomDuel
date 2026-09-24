using KingdomDuel.Domain.Enums;

namespace KingdomDuel.Domain.Rounds;

public enum RoundEventKind
{
    OrderGiven,
    CaravanSent,
    CaravanReturned,
    TradeIncome,
    MoraleDecay,
    MoraleWon,
    MoraleLost,
    Feast,
    BattleJoined,
    BattleLosses,
    WallsHeld,
    WallsBreached,
    WallsRuined,
    WallsRepaired,
    CastleDestroyed,
    Retreated,
    Purchased,
    Recruited,
    MarchedOn,
    TurnedBack
}

public sealed record RoundEvent(
    RoundEventKind Kind,
    int Side,
    int Amount = 0,
    ActionType? Direction = null,
    PurchaseType? Purchase = null);
