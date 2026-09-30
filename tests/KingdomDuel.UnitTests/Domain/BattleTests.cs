using KingdomDuel.Domain;
using KingdomDuel.Domain.Rounds;

namespace KingdomDuel.UnitTests.Domain;

public sealed class BattleTests
{
    [Fact]
    public void Strength_grows_with_the_square_root_of_the_army()
    {
        Battle.StrengthOf(16).ShouldBe(4);
        Battle.StrengthOf(-3).ShouldBe(0);
    }

    [Theory]
    [InlineData(0, 0.5)]
    [InlineData(50, 1.0)]
    [InlineData(100, 1.5)]
    public void Morale_multiplies_strength(int morale, double factor)
    {
        Battle.MoraleFactor(morale).ShouldBe(factor);
    }

    [Fact]
    public void Equal_armies_have_even_odds()
    {
        Battle.LossChanceFor(10, 10).ShouldBe(0.5);
        Battle.LossChanceFor(0, 0).ShouldBe(0.5);
    }

    [Fact]
    public void A_fight_ends_with_one_army_standing_and_every_loss_on_record()
    {
        for (var seed = 0; seed < 300; seed++)
        {
            var random = new Random(seed);
            var army1 = random.Next(1, 40);
            var army2 = random.Next(1, 40);

            var log = Battle.Fight(army1, random.Next(0, 101), army2, random.Next(0, 101), random);

            (log.Army1Left == 0 ^ log.Army2Left == 0).ShouldBeTrue($"seed {seed}");
            log.Ticks.Count.ShouldBe(army1 - log.Army1Left + army2 - log.Army2Left, $"seed {seed}");
        }
    }

    [Theory]
    [InlineData(22, 20, 0.55, 0.70)]
    [InlineData(30, 20, 0.90, 1.00)]
    public void A_bigger_army_wins_more_often_but_not_always(int bigger, int smaller, double low, double high)
    {
        var random = new Random(42);
        const int battles = 4_000;

        var wins = Enumerable.Range(0, battles)
            .Count(_ => Battle.Fight(bigger, smaller, random).Army1Left > 0);

        ((double)wins / battles).ShouldBeInRange(low, high);
    }

    [Fact]
    public void An_archer_on_the_wall_is_worth_three_attackers()
    {
        Battle.WallLossChance.ShouldBe(0.75);
    }

    [Fact]
    public void Archers_shoot_until_the_wall_breaks()
    {
        // six archers shoot every 4/6 seconds: 22 shots fit in 15 seconds
        var log = Battle.Walls(30, 6, 15, new FixedRandom(FixedRandom.FirstLoses));

        log.Army1Left.ShouldBe(8);
        log.Army2Left.ShouldBe(6);
        log.Ticks[^1].At.ShouldBeLessThanOrEqualTo(15);
    }

    [Fact]
    public void Attackers_can_clear_the_wall_before_it_breaks()
    {
        var log = Battle.Walls(30, 6, 15, new FixedRandom(FixedRandom.SecondLoses));

        log.Army1Left.ShouldBe(30);
        log.Army2Left.ShouldBe(0);
    }

    [Theory]
    [InlineData(10, 7)]
    [InlineData(4, 3)]
    [InlineData(0, 0)]
    public void Seventy_percent_of_the_archers_make_it_back_inside(int survivors, int inside)
    {
        Battle.FallBack(survivors).ShouldBe(inside);
    }

    [Theory]
    [InlineData(0, 0, 15)]
    [InlineData(2, 0, 25)]
    [InlineData(0, 2, 5)]
    [InlineData(1, 2, 10)]
    [InlineData(0, 3, 3)]
    [InlineData(-1, 0, 0)]
    public void The_wall_holds_fifteen_seconds_give_or_take(int fortification, int catapults, int seconds)
    {
        GameEngine.BreakingSeconds(fortification, catapults).ShouldBe(seconds);
    }

    [Theory]
    [InlineData(0, 6)]
    [InlineData(1, 8)]
    [InlineData(2, 10)]
    [InlineData(-1, 0)]
    public void Each_fortification_makes_room_for_two_more_archers(int fortification, int capacity)
    {
        GameEngine.WallCapacity(fortification).ShouldBe(capacity);
    }
}
