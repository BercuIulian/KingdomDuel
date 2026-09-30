using KingdomDuel.Domain.Rounds;

namespace KingdomDuel.UnitTests;

public sealed class SentinelTests
{
    [Fact]
    public void Caravan_returns_two_rounds_later()
    {
        GameEngine.CaravanReturnRound(3).ShouldBe(5);
    }
}
