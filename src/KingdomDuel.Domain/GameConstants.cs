using KingdomDuel.Domain.Enums;

namespace KingdomDuel.Domain;

public static class GameConstants
{
    public const int StartingResource = 50;

    public const int DefaultTotalRounds = 8;

    public const int DefaultRoundSeconds = 45;
    public const int MinRoundSeconds = 15;
    public const int MaxRoundSeconds = 180;
    public const int RoundGraceSeconds = 2;
    public const int MissedRoundsBeforeForfeit = 3;

    public const double BattleStrengthExponent = 0.5;
    public const int WallBreakingSeconds = 15;
    public const int WallCapacity = 6;
    public const int WallDefenderWorth = 3;
    public const int WallBreachLossPercent = 30;
    public const double ArcherShotSeconds = 4;

    public const int RetreatSeconds = 3;
    public const int RetreatLossPercent = 10;

    public const int TradeGoldPerSoldier = 1;
    public const double TradeYieldMin = 0.9;
    public const double TradeYieldMax = 1.1;

    public const int MoraleCap = 100;
    public const int MoraleDecayPerRound = 3;

    public const int FeastSmallCost = 5;
    public const int FeastSmallMorale = 4;
    public const int FeastCourtCost = 12;
    public const int FeastCourtMorale = 10;
    public const int FeastGrandCost = 25;
    public const int FeastGrandMorale = 22;

    public static int FeastCost(FeastTier tier) => tier switch
    {
        FeastTier.Small => FeastSmallCost,
        FeastTier.Court => FeastCourtCost,
        FeastTier.Grand => FeastGrandCost,
        _ => 0,
    };

    public static int FeastMorale(FeastTier tier) => tier switch
    {
        FeastTier.Small => FeastSmallMorale,
        FeastTier.Court => FeastCourtMorale,
        FeastTier.Grand => FeastGrandMorale,
        _ => 0,
    };
}
