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

        var gameOver = EvaluateGameOver(after1, after2, input.Round, input.TotalRounds);

        return new RoundOutcome(after1, after2, events, gameOver);
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

        return new SideOutcome(state, caravan);
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
