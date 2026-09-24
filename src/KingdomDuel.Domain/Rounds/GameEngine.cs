using KingdomDuel.Domain.Enums;

namespace KingdomDuel.Domain.Rounds;

public static class GameEngine
{
    public static RoundOutcome Resolve(RoundInput input, Random random)
    {
        var order1 = input.Player1.Order.ClampTo(input.Player1.Kingdom);
        var order2 = input.Player2.Order.ClampTo(input.Player2.Kingdom);

        var events = new List<RoundEvent>();
        Record(1, order1, events);
        Record(2, order2, events);

        var after1 = ResolveSide(1, input.Player1, order1, input.Round, random, events);
        var after2 = ResolveSide(2, input.Player2, order2, input.Round, random, events);

        var battle = PlanBattle(after1, after2, order1, order2, random);

        if (battle is not null)
        {
            return new RoundOutcome(after1, after2, events, GameOverReason.None, battle);
        }

        (after1, after2) = LandRecruits(after1, after2, events);

        return new RoundOutcome(
            after1,
            after2,
            events,
            EvaluateGameOver(after1, after2, input.Round, input.TotalRounds));
    }

    public static RoundOutcome SettleBattle(
        RoundOutcome beforeBattle,
        BattleReplay replay,
        double endsAt,
        int round,
        int totalRounds,
        Retreat? retreat = null)
    {
        var events = beforeBattle.Events.ToList();

        var (troops1, troops2) = replay.AttackerSide == 0
            ? SettleRoad(beforeBattle, replay, endsAt, events)
            : SettleAssault(beforeBattle, replay, endsAt, events);

        if (retreat is { } signal)
        {
            var toll = RetreatToll(replay, endsAt, signal.Side);

            events.Add(new RoundEvent(RoundEventKind.Retreated, signal.Side, toll));

            if (signal.Side == 1)
            {
                troops1 -= toll;
            }
            else
            {
                troops2 -= toll;
            }
        }

        var after1 = beforeBattle.Player1 with
        {
            Kingdom = beforeBattle.Player1.Kingdom with { Troops = Math.Max(0, troops1) },
        };
        var after2 = beforeBattle.Player2 with
        {
            Kingdom = beforeBattle.Player2.Kingdom with { Troops = Math.Max(0, troops2) },
        };

        (after1, after2) = BringWallDown(after1, after2, replay, endsAt, events);
        (after1, after2) = LoseCatapults(after1, after2, replay, endsAt, retreat, events);

        var castle = CastleFallen(replay, endsAt, troops1, troops2);

        if (castle is not GameOverReason.None)
        {
            events.Add(new RoundEvent(
                RoundEventKind.CastleDestroyed,
                castle is GameOverReason.Player1CastleDestroyed ? 1 : 2));

            return new RoundOutcome(after1, after2, events, castle);
        }

        (after1, after2) = MoveMorale(after1, after2, replay, endsAt, retreat, events);
        (after1, after2) = LandRecruits(after1, after2, events);

        return new RoundOutcome(after1, after2, events, EvaluateGameOver(after1, after2, round, totalRounds));
    }

    private static (SideOutcome, SideOutcome) BringWallDown(
        SideOutcome side1,
        SideOutcome side2,
        BattleReplay replay,
        double endsAt,
        List<RoundEvent> events)
    {
        if (replay.AttackerSide == 0 || replay.BreachAt is not { } breach || endsAt < breach)
        {
            return (side1, side2);
        }

        var defenderSide = replay.AttackerSide == 1 ? 2 : 1;
        var defender = defenderSide == 1 ? side1 : side2;

        if (defender.Kingdom.WallIsDown)
        {
            return (side1, side2);
        }

        events.Add(new RoundEvent(RoundEventKind.WallsRuined, defenderSide, defender.Kingdom.Fortification));

        var ruined = defender with
        {
            Kingdom = defender.Kingdom with
            {
                Fortification = GameConstants.WallDownLevel,
                RuinedFrom = defender.Kingdom.Fortification,
            },
        };

        return defenderSide == 1 ? (ruined, side2) : (side1, ruined);
    }

    private static (SideOutcome, SideOutcome) LoseCatapults(
        SideOutcome side1,
        SideOutcome side2,
        BattleReplay replay,
        double endsAt,
        Retreat? retreat,
        List<RoundEvent> events)
    {
        foreach (var side in replay.AttackerSide == 0 ? new[] { 1, 2 } : [replay.AttackerSide])
        {
            var walker = side == 1 ? side1 : side2;

            if (walker.Kingdom.Catapults <= 0)
            {
                continue;
            }

            var ran = retreat?.Side == side;
            var fell = SurvivingExpedition(replay, endsAt, side) <= 0;

            if (!ran && !fell)
            {
                continue;
            }

            events.Add(new RoundEvent(RoundEventKind.CatapultsLost, side, walker.Kingdom.Catapults));

            var stripped = walker with { Kingdom = walker.Kingdom with { Catapults = 0 } };

            if (side == 1)
            {
                side1 = stripped;
            }
            else
            {
                side2 = stripped;
            }
        }

        return (side1, side2);
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

    public static int BreakingSeconds(int fortification, int catapults) =>
        fortification <= GameConstants.WallDownLevel
            ? 0
            : Math.Max(
                GameConstants.MinBreakingSeconds,
                GameConstants.WallBreakingSeconds
                    + fortification * GameConstants.FortifySeconds
                    - catapults * GameConstants.CatapultSeconds);

    public static int WallCapacity(int fortification) =>
        fortification <= GameConstants.WallDownLevel
            ? 0
            : GameConstants.WallCapacity + fortification * GameConstants.FortifyArchers;

    private static (SideOutcome, SideOutcome) MoveMorale(
        SideOutcome side1,
        SideOutcome side2,
        BattleReplay replay,
        double endsAt,
        Retreat? retreat,
        List<RoundEvent> events)
    {
        var (winner, loser) = replay.AttackerSide == 0
            ? RoadVerdict(replay, endsAt, retreat)
            : AssaultVerdict(replay, endsAt);

        var bothLost = winner == 0;

        return (Shift(1, side1), Shift(2, side2));

        SideOutcome Shift(int side, SideOutcome outcome)
        {
            if (side == winner)
            {
                events.Add(new RoundEvent(RoundEventKind.MoraleWon, side, GameConstants.BattleMoraleWin));

                return WithMorale(outcome, GameConstants.BattleMoraleWin);
            }

            if (side != loser && !bothLost)
            {
                return outcome;
            }

            events.Add(new RoundEvent(RoundEventKind.MoraleLost, side, GameConstants.BattleMoraleLoss));

            return WithMorale(outcome, -GameConstants.BattleMoraleLoss);
        }
    }

    private static SideOutcome WithMorale(SideOutcome outcome, int delta) => outcome with
    {
        Kingdom = outcome.Kingdom with
        {
            Morale = Math.Clamp(outcome.Kingdom.Morale + delta, 0, GameConstants.MoraleCap),
        },
    };

    private static (int Winner, int Loser) RoadVerdict(BattleReplay replay, double endsAt, Retreat? retreat)
    {
        var standing = replay.At(endsAt);

        if (retreat is { } signal)
        {
            var other = signal.Side == 1 ? 2 : 1;
            var otherLeft = other == 1 ? standing.Player1Left : standing.Player2Left;

            return otherLeft > 0 ? (other, signal.Side) : (0, 0);
        }

        return standing.Player1Left <= 0 ? (2, 1) : (1, 2);
    }

    private static (int Winner, int Loser) AssaultVerdict(BattleReplay replay, double endsAt)
    {
        var attacker = replay.AttackerSide;
        var defender = attacker == 1 ? 2 : 1;
        var standing = replay.At(endsAt);
        var attackerLeft = attacker == 1 ? standing.Player1Left : standing.Player2Left;
        var breached = replay.BreachAt is { } breach && endsAt >= breach;

        return breached && attackerLeft > 0 ? (attacker, defender) : (defender, attacker);
    }

    private static bool Buys(OrderPlan order, PurchaseType purchase, KingdomState state) =>
        order.Has(purchase) && state.Gold >= GameConstants.PurchaseCost(purchase);

    private static (SideOutcome, SideOutcome) LandRecruits(
        SideOutcome side1,
        SideOutcome side2,
        List<RoundEvent> events)
        => (Land(1, side1, events), Land(2, side2, events));

    private static SideOutcome Land(int side, SideOutcome outcome, List<RoundEvent> events)
    {
        if (outcome.Recruits <= 0)
        {
            return outcome;
        }

        events.Add(new RoundEvent(RoundEventKind.Recruited, side, outcome.Recruits));

        return outcome with
        {
            Kingdom = outcome.Kingdom.With(troopsDelta: outcome.Recruits),
            Recruits = 0,
        };
    }

    public static int MarchChoiceSide(BattleReplay replay, double endsAt, Retreat? retreat)
    {
        if (replay.AttackerSide != 0)
        {
            return 0;
        }

        var standing = replay.At(endsAt);

        if (retreat is { } signal)
        {
            var other = signal.Side == 1 ? 2 : 1;
            var left = other == 1 ? standing.Player1Left : standing.Player2Left;

            return left > 0 ? other : 0;
        }

        if (standing.Player1Left > 0 && standing.Player2Left <= 0) return 1;
        if (standing.Player2Left > 0 && standing.Player1Left <= 0) return 2;

        return 0;
    }

    public static int SurvivingExpedition(BattleReplay replay, double endsAt, int side)
    {
        var start = side == 1 ? replay.Player1Start : replay.Player2Start;

        return Math.Max(0, start - replay.FallsOf(side, endsAt));
    }

    private static (int Troops1, int Troops2) SettleRoad(
        RoundOutcome before,
        BattleReplay replay,
        double endsAt,
        List<RoundEvent> events)
    {
        var lost1 = replay.FallsOf(1, endsAt);
        var lost2 = replay.FallsOf(2, endsAt);

        events.Add(new RoundEvent(RoundEventKind.BattleJoined, 1, replay.Player1Start, ActionType.Attack));
        events.Add(new RoundEvent(RoundEventKind.BattleJoined, 2, replay.Player2Start, ActionType.Attack));
        events.Add(new RoundEvent(RoundEventKind.BattleLosses, 1, lost1));
        events.Add(new RoundEvent(RoundEventKind.BattleLosses, 2, lost2));

        return (before.Player1.Kingdom.Troops - lost1, before.Player2.Kingdom.Troops - lost2);
    }

    private static (int Troops1, int Troops2) SettleAssault(
        RoundOutcome before,
        BattleReplay replay,
        double endsAt,
        List<RoundEvent> events)
    {
        var attackerSide = replay.AttackerSide;
        var defenderSide = attackerSide == 1 ? 2 : 1;

        var army = attackerSide == 1 ? replay.Player1Start : replay.Player2Start;
        var onTheWall = attackerSide == 1 ? replay.Player2Start : replay.Player1Start;
        var attackerBefore = attackerSide == 1 ? before.Player1.Kingdom.Troops : before.Player2.Kingdom.Troops;
        var defenderBefore = attackerSide == 1 ? before.Player2.Kingdom.Troops : before.Player1.Kingdom.Troops;

        events.Add(new RoundEvent(RoundEventKind.BattleJoined, attackerSide, army, ActionType.Attack));
        events.Add(new RoundEvent(RoundEventKind.BattleJoined, defenderSide, onTheWall, ActionType.Defend));

        var attackerLeft = attackerBefore - replay.FallsOf(attackerSide, endsAt);
        int defenderLeft;

        if (replay.BreachAt is { } breach && endsAt >= breach)
        {
            var reinforcements = replay.DefenderInKeep - defenderBefore + onTheWall;

            if (breach > 0)
            {
                events.Add(new RoundEvent(RoundEventKind.WallsBreached, defenderSide, reinforcements, ActionType.Defend));
            }
            events.Add(new RoundEvent(RoundEventKind.BattleJoined, defenderSide, replay.DefenderInKeep, ActionType.Defend));

            defenderLeft = replay.DefenderInKeep - replay.FallsOf(defenderSide, endsAt, BattlePhase.Keep);
        }
        else
        {
            var held = onTheWall - replay.FallsOf(defenderSide, endsAt, BattlePhase.Walls);

            events.Add(new RoundEvent(RoundEventKind.WallsHeld, defenderSide, held, ActionType.Defend));

            defenderLeft = defenderBefore - replay.FallsOf(defenderSide, endsAt);
        }

        events.Add(new RoundEvent(RoundEventKind.BattleLosses, attackerSide, attackerBefore - attackerLeft));
        events.Add(new RoundEvent(RoundEventKind.BattleLosses, defenderSide, defenderBefore - defenderLeft));

        return attackerSide == 1 ? (attackerLeft, defenderLeft) : (defenderLeft, attackerLeft);
    }


    private static int RetreatToll(BattleReplay replay, double endsAt, int side)
    {
        var start = side == 1 ? replay.Player1Start : replay.Player2Start;
        var standing = Math.Max(0, start - replay.FallsOf(side, endsAt));

        return (int)Math.Ceiling(standing * GameConstants.RetreatLossPercent / 100.0);
    }

    private static GameOverReason CastleFallen(BattleReplay replay, double endsAt, int troops1, int troops2)
    {
        if (replay.AttackerSide == 0 || replay.BreachAt is not { } breach || endsAt < breach)
        {
            return GameOverReason.None;
        }

        if (replay.AttackerSide == 1)
        {
            return troops2 <= 0 ? GameOverReason.Player2CastleDestroyed : GameOverReason.None;
        }

        return troops1 <= 0 ? GameOverReason.Player1CastleDestroyed : GameOverReason.None;
    }

    private static BattleReplay? PlanBattle(
        SideOutcome side1,
        SideOutcome side2,
        OrderPlan order1,
        OrderPlan order2,
        Random random)
    {
        var army1 = Math.Min(order1[ActionType.Attack], side1.Kingdom.Troops);
        var army2 = Math.Min(order2[ActionType.Attack], side2.Kingdom.Troops);

        if (army1 > 0 && army2 > 0)
        {
            var log = Battle.Fight(army1, side1.Kingdom.Morale, army2, side2.Kingdom.Morale, random);

            return new BattleReplay(
                0,
                army1,
                army2,
                0,
                null,
                log.Ticks.Count,
                log.Ticks.Select(tick => new BattleFall(tick.At, tick.Army, BattlePhase.Road)).ToList());
        }

        if (army1 > 0)
        {
            return PlanAssault(1, army1, side1.Kingdom, side2, order2, random);
        }

        return army2 > 0 ? PlanAssault(2, army2, side2.Kingdom, side1, order1, random) : null;
    }

    public static BattleReplay PlanAssault(
        int attackerSide,
        int army,
        KingdomState attacker,
        SideOutcome defender,
        OrderPlan defenceOrder,
        Random random)
    {
        var defenderSide = attackerSide == 1 ? 2 : 1;
        var seconds = BreakingSeconds(defender.Kingdom.Fortification, attacker.Catapults);
        var capacity = WallCapacity(defender.Kingdom.Fortification);

        var wall = defender.Kingdom.WallIsDown
            ? 0
            : Math.Min(defenceOrder[ActionType.Defend], defender.Kingdom.Troops);
        var log = Battle.Assault(
            army,
            wall,
            defender.Kingdom.Troops - wall,
            seconds,
            capacity,
            attacker.Morale,
            defender.Kingdom.Morale,
            random);

        var falls = log.Walls.Ticks
            .Select(tick => new BattleFall(tick.At, tick.Army == 1 ? attackerSide : defenderSide, BattlePhase.Walls))
            .ToList();

        var keepDefenders = defender.Kingdom.Troops - log.OnTheWall + log.Reinforcements;

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

        var breachAt = seconds;

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

        var recruits = 0;

        if (Buys(order, PurchaseType.Recruit, state))
        {
            state = state.With(goldDelta: -GameConstants.RecruitCost);
            recruits = GameConstants.RecruitTroops;

            events.Add(new RoundEvent(RoundEventKind.Purchased, side, Purchase: PurchaseType.Recruit));
        }

        if (state.WallIsDown && Buys(order, PurchaseType.Repair, state))
        {
            state = state.With(goldDelta: -GameConstants.RepairCost) with { Fortification = 0 };

            var back = 0;

            if (order.Has(PurchaseType.RepairToTwo) && state.RuinedFrom >= 2
                && state.Gold >= GameConstants.RepairToTwoCost)
            {
                state = state.With(goldDelta: -GameConstants.RepairToTwoCost) with { Fortification = 2 };
                back = 2;
            }
            else if (order.Has(PurchaseType.RepairToOne) && state.RuinedFrom >= 1
                && state.Gold >= GameConstants.RepairToOneCost)
            {
                state = state.With(goldDelta: -GameConstants.RepairToOneCost) with { Fortification = 1 };
                back = 1;
            }

            events.Add(new RoundEvent(RoundEventKind.WallsRepaired, side, back));
        }

        if (Buys(order, PurchaseType.Fortify, state) && !state.WallIsDown
            && state.Fortification < GameConstants.FortifyMaxLevel)
        {
            state = state.With(goldDelta: -GameConstants.FortifyCost) with
            {
                Fortification = state.Fortification + 1,
            };

            events.Add(new RoundEvent(
                RoundEventKind.Purchased,
                side,
                state.Fortification,
                Purchase: PurchaseType.Fortify));
        }

        if (Buys(order, PurchaseType.Catapult, state) && state.Catapults < GameConstants.CatapultMaxLevel)
        {
            state = state.With(goldDelta: -GameConstants.CatapultCost) with
            {
                Catapults = state.Catapults + 1,
            };

            events.Add(new RoundEvent(
                RoundEventKind.Purchased,
                side,
                state.Catapults,
                Purchase: PurchaseType.Catapult));
        }

        return new SideOutcome(state with { RuinedFrom = 0 }, caravan, recruits);
    }

    private static KingdomState Feast(
        int side,
        KingdomState state,
        OrderPlan order,
        SideInput input,
        List<RoundEvent> events)
    {
        if (order.Feast is not { } tier || state.Gold < GameConstants.FeastCost(tier))
        {
            return state;
        }

        var morale = FeastMorale(tier, input.Kingdom.Troops, input.Caravan?.Troops ?? 0, state.Morale);

        state = state.With(goldDelta: -GameConstants.FeastCost(tier), moraleDelta: morale);

        events.Add(new RoundEvent(RoundEventKind.Feast, side, morale));

        return state;
    }

    public static (int Low, int High) TradeRange(int traders) =>
        (TradeYieldAt(traders, GameConstants.TradeYieldMin), TradeYieldAt(traders, GameConstants.TradeYieldMax));

    public static int FeastMorale(FeastTier tier, int present, int away, int morale)
    {
        var whole = present + away;
        var gain = whole <= 0 ? 0 : (int)Math.Round(GameConstants.FeastMorale(tier) * (double)present / whole);

        return Math.Min(gain, Math.Max(0, GameConstants.MoraleCap - morale));
    }

    private static int TradeYield(int traders, Random random)
    {
        var factor = GameConstants.TradeYieldMin
            + random.NextDouble() * (GameConstants.TradeYieldMax - GameConstants.TradeYieldMin);

        return TradeYieldAt(traders, factor);
    }

    private static int TradeYieldAt(int traders, double factor) =>
        Math.Max(1, (int)Math.Round(traders * GameConstants.TradeGoldPerSoldier * factor));

    private static void Record(int side, OrderPlan order, List<RoundEvent> events)
    {
        foreach (var (direction, count) in order.Troops)
        {
            events.Add(new RoundEvent(RoundEventKind.OrderGiven, side, count, direction));
        }
    }
}
