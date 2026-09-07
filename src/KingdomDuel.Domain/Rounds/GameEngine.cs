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

        var after1 = Trade(1, input.Player1.Kingdom, order1, random, events);
        var after2 = Trade(2, input.Player2.Kingdom, order2, random, events);

        var gameOver = EvaluateGameOver(after1, after2, input.Round, input.TotalRounds);

        return new RoundOutcome(after1, after2, events, gameOver);
    }

    public static GameOverReason EvaluateGameOver(
        KingdomState player1,
        KingdomState player2,
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

    private static KingdomState Trade(
        int side,
        KingdomState state,
        OrderPlan order,
        Random random,
        List<RoundEvent> events)
    {
        var traders = order[ActionType.Trade];

        if (traders <= 0)
        {
            return state;
        }

        var yield = GameConstants.TradeYieldMin
            + random.NextDouble() * (GameConstants.TradeYieldMax - GameConstants.TradeYieldMin);

        var gold = Math.Max(1, (int)Math.Round(traders * GameConstants.TradeGoldPerSoldier * yield));

        events.Add(new RoundEvent(RoundEventKind.TradeIncome, side, gold));

        return state.With(goldDelta: gold);
    }

    private static void Record(int side, OrderPlan order, List<RoundEvent> events)
    {
        foreach (var (direction, count) in order.Troops)
        {
            events.Add(new RoundEvent(RoundEventKind.OrderGiven, side, count, direction));
        }
    }
}
