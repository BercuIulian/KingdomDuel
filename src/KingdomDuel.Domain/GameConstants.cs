using KingdomDuel.Domain.Enums;

namespace KingdomDuel.Domain;

public static class GameConstants
{
    public const int StartingGold = 50;
    public const int StartingResource = 50;
    public const int StartingVillagers = 15;
    public const int StartingSoldiers = 10;

    public const int DefaultTotalRounds = 10;
    public const int MinTotalRounds = 8;
    public const int MaxTotalRounds = 30;

    public const int DefaultRoundSeconds = 45;
    public const int MinRoundSeconds = 15;
    public const int MaxRoundSeconds = 180;
    public const int RoundGraceSeconds = 2;
    public const int MissedRoundsBeforeForfeit = 3;
    public const int RematchCountdownSeconds = 3;

    public const double BattleStrengthExponent = 0.5;
    public const int WallBreakingSeconds = 15;
    public const int WallCapacity = 6;
    public const int WallDefenderWorth = 3;
    public const int WallBreachLossPercent = 30;
    public const double ArcherShotSeconds = 4;

    public const int RetreatSeconds = 3;
    public const int RetreatLossPercent = 10;
    public const int MarchChoiceSeconds = 10;
    public const int RallySeconds = 10;

    public const int TradeGoldPerSoldier = 1;
    public const double TradeYieldMin = 1.4;
    public const double TradeYieldMax = 1.6;

    public const int MoraleBaseline = 50;
    public const double MoraleSwing = 0.5;
    public const int BattleMoraleWin = 3;
    public const int BattleMoraleLoss = 5;

    public const int MoraleCap = 100;
    public const int MoraleDecayPerRound = 3;

    public const int VillagerBundleCost = 8;
    public const int VillagersPerBundle = 5;
    public const int ArmCost = 1;
    public const int RallyCost = 2;
    public const double VillagerFeastWeight = 0.5;

    public const int RecruitCost = 12;
    public const int RecruitTroops = 6;

    public const int FortifyCost = 12;
    public const int FortifyMaxLevel = 2;
    public const int FortifySeconds = 5;
    public const int FortifyArchers = 2;

    public const int WallDownLevel = -1;
    public const int RepairCost = 12;
    public const int RepairToOneCost = 4;
    public const int RepairToTwoCost = 7;

    public const int CatapultCost = 15;
    public const int CatapultMaxLevel = 2;
    public const int CatapultSeconds = 5;

    public const int MinBreakingSeconds = 3;

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

    public static int PurchaseCost(PurchaseType purchase) => purchase switch
    {
        PurchaseType.Recruit => RecruitCost,
        PurchaseType.Fortify => FortifyCost,
        PurchaseType.Catapult => CatapultCost,
        PurchaseType.Repair => RepairCost,
        PurchaseType.RepairToOne => RepairToOneCost,
        PurchaseType.RepairToTwo => RepairToTwoCost,
        _ => 0,
    };

    public static int FeastMorale(FeastTier tier) => tier switch
    {
        FeastTier.Small => FeastSmallMorale,
        FeastTier.Court => FeastCourtMorale,
        FeastTier.Grand => FeastGrandMorale,
        _ => 0,
    };

    public static int SpyVillagers(SpyTier tier) => tier switch
    {
        SpyTier.Lone => 1,
        SpyTier.Pair => 3,
        SpyTier.Ring => 5,
        _ => 0,
    };

    public static int SpyUpkeep(SpyTier tier) => SpyVillagers(tier);

    public static double SpyAccuracy(SpyTier tier) => tier switch
    {
        SpyTier.Lone => 0.65,
        SpyTier.Pair => 0.85,
        SpyTier.Ring => 0.95,
        _ => 0,
    };

    public static bool SpiesReadOrders(SpyTier tier) => tier == SpyTier.Ring;

    public static int SpyLevel(SpyTier tier) => tier switch
    {
        SpyTier.Lone => 1,
        SpyTier.Pair => 2,
        SpyTier.Ring => 3,
        _ => 0,
    };

    public static SpyTier? SpyAtLevel(int level) => level switch
    {
        1 => SpyTier.Lone,
        2 => SpyTier.Pair,
        >= 3 => SpyTier.Ring,
        _ => null,
    };

    public static int HuntSoldiers(HuntTier tier) => tier switch
    {
        HuntTier.Scout => 1,
        HuntTier.Patrol => 2,
        HuntTier.Sweep => 3,
        _ => 0,
    };

    public static int HuntCost(HuntTier tier) => tier switch
    {
        HuntTier.Scout => 1,
        HuntTier.Patrol => 2,
        HuntTier.Sweep => 5,
        _ => 0,
    };

    public static int HuntLevels(HuntTier tier) => HuntSoldiers(tier);
}
