using KingdomDuel.Domain.Enums;

namespace KingdomDuel.Domain.Rounds;

public static class GameEngine
{
    public static GameEventType DrawEvent(Random random)
    {
        var values = Enum.GetValues<GameEventType>();

        return values[random.Next(values.Length)];
    }

    public static RoundOutcome Resolve(RoundInput input)
    {
        var order1 = input.Player1.Order.ClampTo(input.Player1.Kingdom);
        var order2 = input.Player2.Order.ClampTo(input.Player2.Kingdom);

        var events = new List<RoundEvent>();
        Record(1, order1, events);
        Record(2, order2, events);

        var after1 = input.Player1.Kingdom;
        var after2 = input.Player2.Kingdom;

        var gameOver = EvaluateGameOver(after1, after2, input.Round, input.TotalRounds);

        if (gameOver is GameOverReason.Player1Collapsed or GameOverReason.BothCollapsed)
        {
            events.Add(new RoundEvent(RoundEventKind.Collapse, 1));
        }

        if (gameOver is GameOverReason.Player2Collapsed or GameOverReason.BothCollapsed)
        {
            events.Add(new RoundEvent(RoundEventKind.Collapse, 2));
        }

        return new RoundOutcome(after1, after2, events, gameOver);
    }

    public static GameOverReason EvaluateGameOver(
        KingdomState player1,
        KingdomState player2,
        int round,
        int totalRounds)
    {
        if (player1.HasCollapsed && player2.HasCollapsed) return GameOverReason.BothCollapsed;
        if (player1.HasCollapsed) return GameOverReason.Player1Collapsed;
        if (player2.HasCollapsed) return GameOverReason.Player2Collapsed;

        if (round >= totalRounds)
        {
            if (player1.TotalPower > player2.TotalPower) return GameOverReason.RoundsExhaustedPlayer1Wins;
            if (player2.TotalPower > player1.TotalPower) return GameOverReason.RoundsExhaustedPlayer2Wins;

            return GameOverReason.RoundsExhaustedDraw;
        }

        return GameOverReason.None;
    }

    private static void Record(int side, OrderPlan order, List<RoundEvent> events)
    {
        foreach (var (direction, count) in order.Troops)
        {
            events.Add(new RoundEvent(RoundEventKind.OrderGiven, side, count, direction));
        }
    }
}
