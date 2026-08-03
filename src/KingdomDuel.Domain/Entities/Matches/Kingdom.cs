using KingdomDuel.Domain.Contracts;

namespace KingdomDuel.Domain.Entities.Matches;

public sealed class Kingdom : Entity
{
    public int Gold { get; set; } = GameConstants.StartingResource;
    public int Troops { get; set; } = GameConstants.StartingResource;
    public int Morale { get; set; } = GameConstants.StartingResource;

    public int DefenseStreak { get; set; }

    public int TotalPower => Gold + Troops + Morale;

    public int EquipmentLevel => Math.Clamp(Gold / GameConstants.GoldPerEquipmentLevel, 0, GameConstants.MaxEquipmentLevel);
}
