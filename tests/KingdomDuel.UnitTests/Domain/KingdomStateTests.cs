using KingdomDuel.Domain.Enums;
using KingdomDuel.Domain.Rounds;
using static KingdomDuel.UnitTests.Domain.GameSetup;

namespace KingdomDuel.UnitTests.Domain;

public sealed class KingdomStateTests
{
    [Fact]
    public void Nothing_drops_below_zero()
    {
        var state = Kingdom(gold: 3, troops: 3, morale: 3, villagers: 3)
            .With(goldDelta: -10, troopsDelta: -10, moraleDelta: -10, villagersDelta: -10);

        state.ShouldBe(Kingdom(gold: 0, troops: 0, morale: 0, villagers: 0));
    }

    [Fact]
    public void Power_is_gold_plus_soldiers_plus_villagers_plus_morale()
    {
        Kingdom(gold: 50, troops: 10, morale: 50, villagers: 15).TotalPower.ShouldBe(125);
    }

    [Fact]
    public void Traders_and_spies_away_still_count_for_power()
    {
        var side = new SideOutcome(
            Kingdom(gold: 50, troops: 10, morale: 50, villagers: 5),
            new Caravan(7, 3),
            Spies: new SpyRing(SpyTier.Pair));

        side.TotalPower.ShouldBe(115 + 7 + 3);
    }

    [Theory]
    [InlineData(-1, true)]
    [InlineData(0, false)]
    [InlineData(2, false)]
    public void Wall_is_down_only_below_level_zero(int fortification, bool down)
    {
        Kingdom(fortification: fortification).WallIsDown.ShouldBe(down);
    }
}
