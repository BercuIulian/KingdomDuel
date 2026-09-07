using KingdomDuel.Domain.Contracts;
using KingdomDuel.Domain.Rounds;

namespace KingdomDuel.Domain.Entities.Matches;

public sealed class Kingdom : Entity
{
    public int Gold { get; set; } = GameConstants.StartingResource;
    public int Troops { get; set; } = GameConstants.StartingResource;
    public int Morale { get; set; } = GameConstants.StartingResource;

    public int TotalPower => Gold + Troops + Morale;

    public KingdomState ToState() => new(Gold, Troops, Morale);

    public void Apply(KingdomState state)
    {
        Gold = state.Gold;
        Troops = state.Troops;
        Morale = state.Morale;
    }
}
