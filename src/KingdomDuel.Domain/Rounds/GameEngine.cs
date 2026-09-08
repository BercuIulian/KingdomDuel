using KingdomDuel.Domain.Enums;

namespace KingdomDuel.Domain.Rounds;

public static class GameEngine
{
    public static GameEventType DrawEvent(Random random)
    {
        var values = Enum.GetValues<GameEventType>();

        return values[random.Next(values.Length)];
    }

    public static RoundOutcome Resolve(RoundInput input, Random random)
    {
        var order1 = input.Player1.Order.ClampTo(input.Player1.Kingdom);
        var order2 = input.Player2.Order.ClampTo(input.Player2.Kingdom);

        var events = new List<RoundEvent>();
        Record(1, order1, events);
        Record(2, order2, events);

        var after1 = ResolveSide(1, input.Player1, order1, input.Round, random, events);
        var after2 = ResolveSide(2, input.Player2, order2, input.Round, random, events);

        var road = MeetOnTheRoad(after1, after2, order1, order2, random, events);
        after1 = road.Player1;
        after2 = road.Player2;

        var storm = StormCastles(after1, after2, order1, order2, random, events);
        after1 = storm.Player1;
        after2 = storm.Player2;

        var gameOver = storm.Over is not GameOverReason.None
            ? storm.Over
            : EvaluateGameOver(after1, after2, input.Round, input.TotalRounds);

        return new RoundOutcome(after1, after2, events, gameOver, road.Battle ?? storm.Battle);
    }

    public static GameOverReason EvaluateGameOver(
        SideOutcome player1,
        SideOutcome player2,
        int round,
        int totalRounds)
    {
        if (round >= totalRounds)
        {
            if (player1.TotalPower > player2.TotalPower) return GameOverReason.RoundsExhaustedPlayer1Wins;
            if (player2.TotalPower > player1.TotalPower) return GameOverReason.RoundsExhaustedPlayer2Wins;

            return GameOverReason.RoundsExhaustedDraw;
        }

        return GameOverReason.None;
    }

    public static int CaravanReturnRound(int round) => round + 2;

    private static (SideOutcome Player1, SideOutcome Player2, GameOverReason Over, BattleReplay? Battle) StormCastles(
        SideOutcome side1,
        SideOutcome side2,
        OrderPlan order1,
        OrderPlan order2,
        Random random,
        List<RoundEvent> events)
    {
        if (order1[ActionType.Attack] > 0 && order2[ActionType.Attack] <= 0)
        {
            var (attacker, defender, over, battle) = Storm(1, side1, side2, order1, order2, random, events);

            return (attacker, defender, over, battle);
        }

        if (order2[ActionType.Attack] > 0 && order1[ActionType.Attack] <= 0)
        {
            var (attacker, defender, over, battle) = Storm(2, side2, side1, order2, order1, random, events);

            return (defender, attacker, over, battle);
        }

        return (side1, side2, GameOverReason.None, null);
    }

    private static (SideOutcome Attacker, SideOutcome Defender, GameOverReason Over, BattleReplay? Battle) Storm(
        int attackerSide,
        SideOutcome attacker,
        SideOutcome defender,
        OrderPlan attackOrder,
        OrderPlan defenceOrder,
        Random random,
        List<RoundEvent> events)
    {
        var defenderSide = attackerSide == 1 ? 2 : 1;

        var army = Math.Min(attackOrder[ActionType.Attack], attacker.Kingdom.Troops);
        var wall = Math.Min(defenceOrder[ActionType.Defend], defender.Kingdom.Troops);
        var garrison = defender.Kingdom.Troops - wall;

        var log = Battle.Assault(army, wall, garrison, random);

        events.Add(new RoundEvent(RoundEventKind.BattleJoined, attackerSide, army, ActionType.Attack));
        events.Add(new RoundEvent(RoundEventKind.BattleJoined, defenderSide, log.OnTheWall, ActionType.Defend));

        var keepDefenders = defender.Kingdom.Troops - log.OnTheWall + log.Reinforcements;

        if (log.Keep is null)
        {
            events.Add(new RoundEvent(RoundEventKind.WallsHeld, defenderSide, log.Walls.Army2Left, ActionType.Defend));
        }
        else
        {
            events.Add(new RoundEvent(RoundEventKind.WallsBreached, defenderSide, log.Reinforcements, ActionType.Defend));
            events.Add(new RoundEvent(RoundEventKind.BattleJoined, defenderSide, keepDefenders, ActionType.Defend));
        }

        var attackerLosses = army - log.AttackersLeft;
        var defenderLosses = defender.Kingdom.Troops - log.DefendersLeft;

        events.Add(new RoundEvent(RoundEventKind.BattleLosses, attackerSide, attackerLosses));
        events.Add(new RoundEvent(RoundEventKind.BattleLosses, defenderSide, defenderLosses));

        if (log.CastleDestroyed)
        {
            events.Add(new RoundEvent(RoundEventKind.CastleDestroyed, defenderSide));
        }

        return (
            attacker with { Kingdom = attacker.Kingdom.With(troopsDelta: -attackerLosses) },
            defender with { Kingdom = defender.Kingdom.With(troopsDelta: -defenderLosses) },
            log.CastleDestroyed
                ? defenderSide == 1 ? GameOverReason.Player1CastleDestroyed : GameOverReason.Player2CastleDestroyed
                : GameOverReason.None,
            AssaultReplay(attackerSide, army, keepDefenders, log));
    }

    private static BattleReplay AssaultReplay(int attackerSide, int army, int keepDefenders, AssaultLog log)
    {
        var defenderSide = attackerSide == 1 ? 2 : 1;
        var falls = log.Walls.Ticks
            .Select(tick => new BattleFall(tick.At, tick.Army == 1 ? attackerSide : defenderSide, BattlePhase.Walls))
            .ToList();

        if (log.Keep is null)
        {
            return new BattleReplay(
                attackerSide,
                attackerSide == 1 ? army : log.OnTheWall,
                attackerSide == 1 ? log.OnTheWall : army,
                keepDefenders,
                null,
                falls.Count == 0 ? 0 : falls[^1].At,
                falls);
        }

        var breachAt = GameConstants.WallBreakingSeconds;

        falls.AddRange(log.Keep.Ticks.Select(tick => new BattleFall(
            breachAt + tick.At,
            tick.Army == 1 ? attackerSide : defenderSide,
            BattlePhase.Keep)));

        return new BattleReplay(
            attackerSide,
            attackerSide == 1 ? army : log.OnTheWall,
            attackerSide == 1 ? log.OnTheWall : army,
            keepDefenders,
            breachAt,
            breachAt + log.Keep.Ticks.Count,
            falls);
    }

    private static (SideOutcome Player1, SideOutcome Player2, BattleReplay? Battle) MeetOnTheRoad(
        SideOutcome side1,
        SideOutcome side2,
        OrderPlan order1,
        OrderPlan order2,
        Random random,
        List<RoundEvent> events)
    {
        var army1 = Math.Min(order1[ActionType.Attack], side1.Kingdom.Troops);
        var army2 = Math.Min(order2[ActionType.Attack], side2.Kingdom.Troops);

        if (army1 <= 0 || army2 <= 0)
        {
            return (side1, side2, null);
        }

        var log = Battle.Fight(army1, army2, random);

        events.Add(new RoundEvent(RoundEventKind.BattleJoined, 1, army1, ActionType.Attack));
        events.Add(new RoundEvent(RoundEventKind.BattleJoined, 2, army2, ActionType.Attack));
        events.Add(new RoundEvent(RoundEventKind.BattleLosses, 1, army1 - log.Army1Left));
        events.Add(new RoundEvent(RoundEventKind.BattleLosses, 2, army2 - log.Army2Left));

        return (
            side1 with { Kingdom = side1.Kingdom.With(troopsDelta: -(army1 - log.Army1Left)) },
            side2 with { Kingdom = side2.Kingdom.With(troopsDelta: -(army2 - log.Army2Left)) },
            new BattleReplay(
                0,
                army1,
                army2,
                0,
                null,
                log.Ticks.Count,
                log.Ticks.Select(tick => new BattleFall(tick.At, tick.Army, BattlePhase.Road)).ToList()));
    }

    private static SideOutcome ResolveSide(
        int side,
        SideInput input,
        OrderPlan order,
        int round,
        Random random,
        List<RoundEvent> events)
    {
        var state = input.Kingdom;
        var caravan = input.Caravan;

        if (caravan is { } away && away.AvailableInRound <= round + 1)
        {
            var gold = TradeYield(away.Troops, random);

            state = state.With(goldDelta: gold, troopsDelta: away.Troops);
            caravan = null;

            events.Add(new RoundEvent(RoundEventKind.CaravanReturned, side, away.Troops, ActionType.Trade));
            events.Add(new RoundEvent(RoundEventKind.TradeIncome, side, gold));
        }

        var traders = order[ActionType.Trade];

        if (traders > 0 && input.Caravan is null)
        {
            state = state.With(troopsDelta: -traders);
            caravan = new Caravan(traders, CaravanReturnRound(round));

            events.Add(new RoundEvent(RoundEventKind.CaravanSent, side, traders, ActionType.Trade));
        }

        state = state.With(moraleDelta: -GameConstants.MoraleDecayPerRound);
        events.Add(new RoundEvent(RoundEventKind.MoraleDecay, side, GameConstants.MoraleDecayPerRound));

        state = Feast(side, state, order, input, events);

        return new SideOutcome(state, caravan);
    }

    private static KingdomState Feast(
        int side,
        KingdomState state,
        OrderPlan order,
        SideInput input,
        List<RoundEvent> events)
    {
        if (order.Feast is not { } tier || state.Gold < order.GoldCost)
        {
            return state;
        }

        var present = input.Kingdom.Troops;
        var whole = present + (input.Caravan?.Troops ?? 0);
        var morale = whole <= 0
            ? 0
            : (int)Math.Round(GameConstants.FeastMorale(tier) * (double)present / whole);

        state = state.With(goldDelta: -order.GoldCost, moraleDelta: morale);

        if (state.Morale > GameConstants.MoraleCap)
        {
            state = state with { Morale = GameConstants.MoraleCap };
        }

        events.Add(new RoundEvent(RoundEventKind.Feast, side, morale));

        return state;
    }

    private static int TradeYield(int traders, Random random)
    {
        var factor = GameConstants.TradeYieldMin
            + random.NextDouble() * (GameConstants.TradeYieldMax - GameConstants.TradeYieldMin);

        return Math.Max(1, (int)Math.Round(traders * GameConstants.TradeGoldPerSoldier * factor));
    }

    private static void Record(int side, OrderPlan order, List<RoundEvent> events)
    {
        foreach (var (direction, count) in order.Troops)
        {
            events.Add(new RoundEvent(RoundEventKind.OrderGiven, side, count, direction));
        }
    }
}
