namespace KingdomDuel.Domain.Enums;

/// <summary>
/// The four actions a kingdom can take in a round. See RULES.md for the full
/// description of what each action costs and yields.
/// </summary>
public enum ActionType
{
    Attack,
    Defend,
    Trade,
    Diplomacy
}
