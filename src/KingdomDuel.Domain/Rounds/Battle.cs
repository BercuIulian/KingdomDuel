using KingdomDuel.Domain;

namespace KingdomDuel.Domain.Rounds;

public sealed record BattleTick(int Army, double At);

public sealed record BattleLog(int Army1Left, int Army2Left, IReadOnlyList<BattleTick> Ticks);

public sealed record AssaultLog(
    int OnTheWall,
    BattleLog Walls,
    int Reinforcements,
    BattleLog? Keep,
    int AttackersLeft,
    int DefendersLeft,
    bool CastleDestroyed);

public static class Battle
{
    public static double WallLossChance =>
        (double)GameConstants.WallDefenderWorth / (GameConstants.WallDefenderWorth + 1);

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
            var at = ticks.Count + 1;

            if (random.NextDouble() < LossChanceFor(left1, left2))
            {
                left1 -= 1;
                ticks.Add(new BattleTick(1, at));
            }
            else
            {
                left2 -= 1;
                ticks.Add(new BattleTick(2, at));
            }
        }

        return new BattleLog(left1, left2, ticks);
    }

    public static AssaultLog Assault(int attackers, int wallDefenders, int garrison, Random random) =>
        Assault(
            attackers,
            wallDefenders,
            garrison,
            GameConstants.WallBreakingSeconds,
            GameConstants.WallCapacity,
            random);

    public static AssaultLog Assault(
        int attackers,
        int wallDefenders,
        int garrison,
        int breakingSeconds,
        int capacity,
        Random random)
    {
        var onTheWall = Math.Clamp(wallDefenders, 0, capacity);
        var inside = garrison + Math.Max(0, wallDefenders - onTheWall);

        var walls = StormWalls(attackers, onTheWall, breakingSeconds, random);

        if (walls.Army1Left <= 0)
        {
            return new AssaultLog(onTheWall, walls, 0, null, 0, walls.Army2Left + inside, false);
        }

        var reinforcements = FallBack(walls.Army2Left);
        var keep = Fight(walls.Army1Left, inside + reinforcements, random);

        return new AssaultLog(
            onTheWall,
            walls,
            reinforcements,
            keep,
            keep.Army1Left,
            keep.Army2Left,
            keep.Army2Left <= 0);
    }

    private static BattleLog StormWalls(int attackers, int onTheWall, int breakingSeconds, Random random)
    {
        var left1 = Math.Max(0, attackers);
        var left2 = onTheWall;
        var ticks = new List<BattleTick>();
        var clock = 0.0;

        while (left1 > 0 && left2 > 0)
        {
            clock += GameConstants.ArcherShotSeconds / left2;
            var at = Math.Round(clock, 2);

            if (clock > breakingSeconds)
            {
                break;
            }

            if (random.NextDouble() < WallLossChance)
            {
                left1 -= 1;
                ticks.Add(new BattleTick(1, at));
            }
            else
            {
                left2 -= 1;
                ticks.Add(new BattleTick(2, at));
            }
        }

        return new BattleLog(left1, left2, ticks);
    }

    private static int FallBack(int wallSurvivors) =>
        wallSurvivors - wallSurvivors * GameConstants.WallBreachLossPercent / 100;
}
