using KingdomDuel.Domain;
using KingdomDuel.Domain.Enums;
using KingdomDuel.Domain.Rounds;

namespace KingdomDuel.UnitTests.Domain;

internal static class GameSetup
{
    public static KingdomState Kingdom(
        int gold = 50,
        int troops = 10,
        int morale = 50,
        int villagers = 15,
        int fortification = 0,
        int catapults = 0,
        int ruinedFrom = 0) =>
        new(gold, troops, morale, villagers, fortification, catapults, ruinedFrom);

    public static OrderPlan Order(
        int attack = 0,
        int defend = 0,
        int trade = 0,
        FeastTier? feast = null,
        PurchaseType[]? buy = null,
        int hired = 0,
        int armed = 0,
        SpyTier? spies = null,
        bool recallSpies = false,
        HuntTier? hunt = null) =>
        new(
            new Dictionary<ActionType, int>
            {
                [ActionType.Attack] = attack,
                [ActionType.Defend] = defend,
                [ActionType.Trade] = trade,
            },
            feast,
            buy,
            hired,
            armed,
            spies,
            recallSpies,
            hunt);

    public static SideInput Side(
        KingdomState? kingdom = null,
        OrderPlan? order = null,
        Caravan? caravan = null,
        SpyRing? spies = null) =>
        new(kingdom ?? Kingdom(), order ?? OrderPlan.Empty, caravan, spies);

    public static RoundOutcome Resolve(
        SideInput? player1 = null,
        SideInput? player2 = null,
        int round = 1,
        int totalRounds = 10,
        Random? random = null) =>
        GameEngine.Resolve(
            new RoundInput(round, totalRounds, player1 ?? Side(), player2 ?? Side()),
            random ?? new Random(1));

    // Plays a round the way the server does when nobody clicks anything:
    // no retreat, nobody rallied at the breach, the field winner turns back.
    public static RoundOutcome Play(
        SideInput? player1 = null,
        SideInput? player2 = null,
        int round = 1,
        int totalRounds = 10,
        Random? random = null)
    {
        random ??= new Random(1);

        var outcome = Resolve(player1, player2, round, totalRounds, random);

        return outcome.Battle is { } battle
            ? Settle(outcome, battle, round, totalRounds, random)
            : outcome;
    }

    public static RoundOutcome Settle(
        RoundOutcome beforeBattle,
        BattleReplay replay,
        int round,
        int totalRounds,
        Random random,
        int rallied = 0,
        Retreat? retreat = null)
    {
        var before = beforeBattle;

        if (GameEngine.KeepToCome(replay, replay.Duration, retreat))
        {
            var defenderSide = replay.AttackerSide == 1 ? 2 : 1;

            if (rallied > 0)
            {
                before = GameEngine.Rally(before, defenderSide, rallied);
            }

            var attacker = replay.AttackerSide == 1 ? before.Player1 : before.Player2;
            var defender = defenderSide == 1 ? before.Player1 : before.Player2;

            replay = GameEngine.StormKeep(replay, rallied, attacker.Kingdom.Morale, defender.Kingdom.Morale, random);
        }

        var endsAt = retreat is { } signal
            ? Math.Min(signal.At + GameConstants.RetreatSeconds, replay.Duration)
            : replay.Duration;

        var settled = GameEngine.SettleBattle(before, replay, endsAt, round, totalRounds, retreat);

        return GameEngine.MarchChoiceSide(replay, endsAt, retreat) == 0
            ? settled
            : GameEngine.CloseRound(settled, round, totalRounds);
    }

    public static int Count(this RoundOutcome outcome, RoundEventKind kind, int side) =>
        outcome.Events.Where(e => e.Kind == kind && e.Side == side).Sum(e => e.Amount);

    public static bool Has(this RoundOutcome outcome, RoundEventKind kind, int side) =>
        outcome.Events.Any(e => e.Kind == kind && e.Side == side);
}

internal sealed class FixedRandom(double value) : Random
{
    public const double FirstLoses = 0.0;
    public const double SecondLoses = 0.999;

    public override double NextDouble() => value;
}
