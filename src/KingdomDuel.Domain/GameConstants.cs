namespace KingdomDuel.Domain;

public static class GameConstants
{
    public const int StartingResource = 50;

    public const int DefaultTotalRounds = 8;

    public const int DefaultRoundSeconds = 45;
    public const int MinRoundSeconds = 15;
    public const int MaxRoundSeconds = 180;

    public const int GoldPerEquipmentLevel = 20;
    public const int MaxEquipmentLevel = 5;

    public const int AttackTroopCost = 10;
    public const int AttackTroopCostDuringPeaceRumors = 20;

    public const int DefenseBaseGoldCost = 5;
    public const int DefenseGoldCostStep = 3;
    public const int DefenseBaseMoraleGain = 5;
    public const int DefenseMoraleGainStep = 2;
    public const int DefenseMinMoraleGain = 1;
    public const int StagnationMoralePenalty = 4;

    public const int TradeBaseGoldGain = 15;
    public const int TradeDroughtGoldGain = 7;
    public const int TradeHarvestGoldGain = 30;
    public const int TradeMoraleGain = 2;

    public const int DiplomacyGoldCost = 5;
    public const int DiplomacyBaseMoraleGain = 10;
    public const int DiplomacyFairMoraleGain = 20;
    public const int DiplomacyAllianceGoldBonus = 10;

    public const int RebellionMoraleThreshold = 30;
    public const int RebellionTroopLoss = 10;

    public const int MutualClashBaseTroopLoss = 15;
    public const int MutualClashMaxMoraleShiftFactor = 5;
    public const int MutualClashMoralePerShiftPoint = 10;
    public const int MutualClashMoraleLoss = 5;

    public const int BreakthroughStealGold = 12;
    public const int BreakthroughStealTroops = 8;
    public const int RepelledExtraTroopLossBase = 10;
    public const int RepelledExtraTroopLossMin = 3;
    public const int RepelledDefenderMoraleBase = 5;

    public const int UndefendedStealGold = 15;
    public const int UndefendedStealTroops = 10;
    public const int UndefendedDiplomacyStealGold = 20;
    public const int UndefendedDiplomacyStealTroops = 15;
    public const int UndefendedAttackerMoraleGain = 5;

    public const double BreakthroughBaseChance = 0.10;
    public const double BreakthroughChancePerLevel = 0.08;
    public const double BreakthroughMinChance = 0.05;
    public const double BreakthroughMaxChance = 0.75;
}
