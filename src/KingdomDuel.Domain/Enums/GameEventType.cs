namespace KingdomDuel.Domain.Enums;

/// <summary>
/// Random event drawn at the start of every round. Modifies the cost/yield
/// of actions for that round only. See RULES.md for exact effects.
/// </summary>
public enum GameEventType
{
    None,
    Drought,
    Harvest,
    Rebellion,
    PeaceRumors,
    DiplomaticFair
}
