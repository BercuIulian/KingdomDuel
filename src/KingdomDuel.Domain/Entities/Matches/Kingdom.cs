using KingdomDuel.Domain.Contracts;

namespace KingdomDuel.Domain.Entities.Matches;

/// <summary>
/// Mutable resource pool for one side of a match. Gold, Troops and Morale
/// persist and accumulate across rounds - they are not reset each round.
/// Persisted as its own "kingdoms" table, 1:1 with the owning PlayerSlot.
/// </summary>
public sealed class Kingdom : Entity
{
    public int Gold { get; set; } = GameConstants.StartingResource;
    public int Troops { get; set; } = GameConstants.StartingResource;
    public int Morale { get; set; } = GameConstants.StartingResource;

    /// <summary>How many consecutive rounds (including the current one) this side has chosen Defend.</summary>
    public int DefenseStreak { get; set; }

    public int TotalPower => Gold + Troops + Morale;

    /// <summary>Gold-funded equipment/fortification level, 0-5. See RULES.md.</summary>
    public int EquipmentLevel => Math.Clamp(Gold / GameConstants.GoldPerEquipmentLevel, 0, GameConstants.MaxEquipmentLevel);
}
