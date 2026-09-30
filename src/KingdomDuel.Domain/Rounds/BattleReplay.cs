namespace KingdomDuel.Domain.Rounds;

public enum BattlePhase
{
    Road,
    Walls,
    Keep
}

public sealed record BattleFall(double At, int Side, BattlePhase Phase);

public sealed record BattleSnapshot(BattlePhase Phase, int Player1Left, int Player2Left, bool Over);

public sealed record BattleReplay(
    int AttackerSide,
    int Player1Start,
    int Player2Start,
    int DefenderInKeep,
    double? BreachAt,
    double Duration,
    IReadOnlyList<BattleFall> Falls,
    int Searching = 0)
{
    public BattleSnapshot At(double seconds)
    {
        var side1 = Player1Start;
        var side2 = Player2Start;

        foreach (var fall in Falls)
        {
            if (fall.Phase == BattlePhase.Keep || fall.At > seconds)
            {
                continue;
            }

            if (fall.Side == 1)
            {
                side1 -= 1;
            }
            else
            {
                side2 -= 1;
            }
        }

        if (BreachAt is not { } breach || seconds < breach)
        {
            var opening = AttackerSide == 0 ? BattlePhase.Road : BattlePhase.Walls;

            return new BattleSnapshot(opening, Math.Max(0, side1), Math.Max(0, side2), seconds >= Duration);
        }

        if (AttackerSide == 1)
        {
            side2 = DefenderInKeep;
        }
        else
        {
            side1 = DefenderInKeep;
        }

        foreach (var fall in Falls)
        {
            if (fall.Phase != BattlePhase.Keep || fall.At > seconds)
            {
                continue;
            }

            if (fall.Side == 1)
            {
                side1 -= 1;
            }
            else
            {
                side2 -= 1;
            }
        }

        return new BattleSnapshot(BattlePhase.Keep, Math.Max(0, side1), Math.Max(0, side2), seconds >= Duration);
    }

    public int FallsOf(int side, double until) =>
        Falls.Count(fall => fall.Side == side && fall.At <= until);

    public int FallsOf(int side, double until, BattlePhase phase) =>
        Falls.Count(fall => fall.Side == side && fall.Phase == phase && fall.At <= until);

    public IReadOnlyList<BattleFall> FallsUntil(double seconds) =>
        Falls.Where(fall => fall.At <= seconds).ToList();
}
