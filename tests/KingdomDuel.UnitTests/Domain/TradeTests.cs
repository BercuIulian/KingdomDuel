using KingdomDuel.Domain.Rounds;
using static KingdomDuel.UnitTests.Domain.GameSetup;

namespace KingdomDuel.UnitTests.Domain;

public sealed class TradeTests
{
    [Fact]
    public void Traders_leave_the_village_and_come_back_two_rounds_later()
    {
        var outcome = Resolve(Side(Kingdom(villagers: 15), Order(trade: 10)), round: 1);

        outcome.Player1.Kingdom.Villagers.ShouldBe(5);
        outcome.Player1.Caravan.ShouldBe(new Caravan(10, 3));
        outcome.Has(RoundEventKind.CaravanSent, 1).ShouldBeTrue();
    }

    [Fact]
    public void Caravan_comes_home_with_gold_at_the_end_of_the_round_before_it_is_due()
    {
        var outcome = Resolve(
            Side(Kingdom(gold: 50, villagers: 5), caravan: new Caravan(10, 3)),
            round: 2,
            random: new FixedRandom(0));

        outcome.Player1.Caravan.ShouldBeNull();
        outcome.Player1.Kingdom.Villagers.ShouldBe(15);
        outcome.Player1.Kingdom.Gold.ShouldBe(50 + 14);
        outcome.Count(RoundEventKind.TradeIncome, 1).ShouldBe(14);
    }

    [Fact]
    public void Caravan_stays_on_the_road_until_it_is_due()
    {
        var caravan = new Caravan(10, 4);

        var outcome = Resolve(Side(Kingdom(villagers: 5), caravan: caravan), round: 2);

        outcome.Player1.Caravan.ShouldBe(caravan);
        outcome.Player1.Kingdom.Villagers.ShouldBe(5);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(3)]
    public void A_second_caravan_waits_for_the_first(int availableInRound)
    {
        var outcome = Resolve(
            Side(Kingdom(villagers: 10), Order(trade: 5), caravan: new Caravan(5, availableInRound)),
            round: 2);

        outcome.Has(RoundEventKind.CaravanSent, 1).ShouldBeFalse();
    }

    [Theory]
    [InlineData(10, 14, 16)]
    [InlineData(7, 10, 11)]
    [InlineData(1, 1, 2)]
    public void Each_trader_brings_between_one_point_four_and_one_point_six_gold(int traders, int low, int high)
    {
        GameEngine.TradeRange(traders).ShouldBe((low, high));
    }

    [Fact]
    public void Trade_income_always_lands_inside_the_advertised_range()
    {
        for (var seed = 0; seed < 500; seed++)
        {
            var traders = seed % 30 + 1;
            var (low, high) = GameEngine.TradeRange(traders);

            var outcome = Resolve(
                Side(Kingdom(gold: 0, villagers: 0), caravan: new Caravan(traders, 2)),
                round: 1,
                random: new Random(seed));

            outcome.Count(RoundEventKind.TradeIncome, 1).ShouldBeInRange(low, high, $"seed {seed}");
        }
    }
}
