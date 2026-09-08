using KingdomDuel.Domain;

namespace KingdomDuel.Domain.Rounds;

public sealed record BattleTick(int Army);

public sealed record BattleLog(int Army1Left, int Army2Left, IReadOnlyList<BattleTick> Ticks);

public static class Battle
{
    public static double StrengthOf(int army) => Math.Pow(Math.Max(0, army), GameConstants.BattleStrengthExponent);

    public static double LossChanceFor(int army, int opponent)
    {
        var mine = StrengthOf(army);
        var theirs = StrengthOf(opponent);

        return mine + theirs <= 0 ? 0.5 : theirs / (mine + theirs);
    }

    public static BattleLog Fight(int army1, int army2, Random random)
    {
        var left1 = Math.Max(0, army1);
        var left2 = Math.Max(0, army2);
        var ticks = new List<BattleTick>();

        while (left1 > 0 && left2 > 0)
        {
            if (random.NextDouble() < LossChanceFor(left1, left2))
            {
                left1 -= 1;
                ticks.Add(new BattleTick(1));
            }
            else
            {
                left2 -= 1;
                ticks.Add(new BattleTick(2));
            }
        }

        return new BattleLog(left1, left2, ticks);
    }
}
