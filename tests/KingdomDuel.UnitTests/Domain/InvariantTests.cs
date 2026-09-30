using System.Text.Json;
using KingdomDuel.Domain;
using KingdomDuel.Domain.Enums;
using KingdomDuel.Domain.Rounds;
using static KingdomDuel.UnitTests.Domain.GameSetup;

namespace KingdomDuel.UnitTests.Domain;

// Whole matches played on random orders. A failure names the seed, and
// the same seed replays the exact same match.
public sealed class InvariantTests
{
    private const int Matches = 400;

    public static TheoryData<int> Seeds() => new(Enumerable.Range(0, Matches));

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Kingdoms_stay_within_bounds(int seed)
    {
        foreach (var (_, after) in PlayMatch(seed))
        {
            foreach (var side in new[] { after.Player1, after.Player2 })
            {
                var kingdom = side.Kingdom;

                kingdom.Gold.ShouldBeGreaterThanOrEqualTo(0);
                kingdom.Troops.ShouldBeGreaterThanOrEqualTo(0);
                kingdom.Villagers.ShouldBeGreaterThanOrEqualTo(0);
                kingdom.Morale.ShouldBeInRange(0, GameConstants.MoraleCap);
                kingdom.Fortification.ShouldBeInRange(GameConstants.WallDownLevel, GameConstants.FortifyMaxLevel);
                kingdom.Catapults.ShouldBeInRange(0, GameConstants.CatapultMaxLevel);
                (side.Caravan?.Troops ?? 1).ShouldBeGreaterThan(0);
            }
        }
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Nobody_appears_or_vanishes_without_an_event(int seed)
    {
        foreach (var (before, after) in PlayMatch(seed))
        {
            for (var side = 1; side <= 2; side++)
            {
                var was = People(side == 1 ? before.Player1 : before.Player2);
                var outcome = side == 1 ? after.Player1 : after.Player2;
                var now = People(outcome) + outcome.Recruits + outcome.Hired;

                var expected = was
                    + after.Count(RoundEventKind.VillagersHired, side)
                    + after.Events.Count(e => e.Kind == RoundEventKind.Purchased && e.Side == side
                        && e.Purchase == PurchaseType.Recruit) * GameConstants.RecruitTroops
                    - after.Count(RoundEventKind.BattleLosses, side)
                    - after.Count(RoundEventKind.Retreated, side)
                    - after.Count(RoundEventKind.SpiesKilled, side);

                now.ShouldBe(expected, $"side {side}");
            }
        }
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void The_same_seed_plays_the_same_match(int seed)
    {
        var first = JsonSerializer.Serialize(PlayMatch(seed).Select(r => r.After));
        var second = JsonSerializer.Serialize(PlayMatch(seed).Select(r => r.After));

        second.ShouldBe(first);
    }

    private static int People(SideInput side) =>
        side.Kingdom.Troops + side.Kingdom.Villagers + (side.Caravan?.Troops ?? 0) + (side.Spies?.Villagers ?? 0);

    private static int People(SideOutcome side) =>
        side.Kingdom.Troops + side.Kingdom.Villagers + (side.Caravan?.Troops ?? 0) + (side.Spies?.Villagers ?? 0);

    private static List<(RoundInput Before, RoundOutcome After)> PlayMatch(int seed)
    {
        var random = new Random(seed);
        var totalRounds = random.Next(GameConstants.MinTotalRounds, GameConstants.MaxTotalRounds + 1);
        var start = Kingdom(
            gold: random.Next(0, 200),
            troops: random.Next(1, 60),
            morale: random.Next(1, 101),
            villagers: random.Next(1, 60));

        SideInput side1 = Side(start);
        SideInput side2 = Side(start);
        var rounds = new List<(RoundInput, RoundOutcome)>();

        for (var round = 1; round <= totalRounds; round++)
        {
            var input = new RoundInput(
                round,
                totalRounds,
                side1 with { Order = RandomOrder(side1.Kingdom, random) },
                side2 with { Order = RandomOrder(side2.Kingdom, random) });

            var outcome = GameEngine.Resolve(input, random);

            if (outcome.Battle is { } battle)
            {
                outcome = SettleRandomly(outcome, battle, round, totalRounds, random);
            }

            rounds.Add((input, outcome));

            if (outcome.GameOverReason is not GameOverReason.None)
            {
                break;
            }

            side1 = new SideInput(outcome.Player1.Kingdom, OrderPlan.Empty, outcome.Player1.Caravan, outcome.Player1.Spies);
            side2 = new SideInput(outcome.Player2.Kingdom, OrderPlan.Empty, outcome.Player2.Caravan, outcome.Player2.Spies);
        }

        return rounds;
    }

    private static RoundOutcome SettleRandomly(
        RoundOutcome before,
        BattleReplay replay,
        int round,
        int totalRounds,
        Random random)
    {
        Retreat? retreat = null;
        var canRetreat = replay.AttackerSide == 0 ? new[] { 1, 2 } : [replay.AttackerSide];

        if (random.Next(4) == 0 && replay.Duration > 0)
        {
            var at = Math.Round(random.NextDouble() * replay.Duration, 2);

            // there is no retreat while the bell rings at the breach
            if (replay.BreachAt is not { } breach || at < breach)
            {
                retreat = new Retreat(canRetreat[random.Next(canRetreat.Length)], at);
            }
        }

        var rallied = 0;

        if (retreat is null && GameEngine.KeepToCome(replay, replay.Duration, null))
        {
            var defender = replay.AttackerSide == 1 ? before.Player2 : before.Player1;
            rallied = random.Next(0, GameEngine.RallyCeiling(defender.Kingdom) + 1);
        }

        return Settle(before, replay, round, totalRounds, random, rallied, retreat);
    }

    private static OrderPlan RandomOrder(KingdomState kingdom, Random random)
    {
        var troops = kingdom.Troops;

        return Order(
            attack: random.Next(3) == 0 ? random.Next(0, troops + 1) : 0,
            defend: random.Next(0, troops + 1),
            trade: random.Next(0, kingdom.Villagers + 1),
            feast: random.Next(5) is var f and < 3 ? (FeastTier)f : null,
            buy: Enum.GetValues<PurchaseType>().Where(_ => random.Next(4) == 0).ToArray(),
            hired: random.Next(0, 3),
            armed: random.Next(0, 6),
            spies: random.Next(6) is var s and < 3 ? (SpyTier)s : null,
            recallSpies: random.Next(10) == 0,
            hunt: random.Next(6) is var h and < 3 ? (HuntTier)h : null);
    }
}
