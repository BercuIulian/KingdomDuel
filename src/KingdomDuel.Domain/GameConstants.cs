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

    public const int GoldPerEquipmentLevel = 20;
    public const int MaxEquipmentLevel = 5;

    // Atacul nu are un cost fix: atacatorul alege câte trupe trimite, între minim
    // și plafonul rundei. Trupele plecate nu se pierd toate — vezi pierderile de
    // expediție mai jos.
    public const int MinAttackTroops = 1;
    public const int MaxAttackTroops = 15;
    public const int MaxAttackTroopsDuringPeaceRumors = 8;

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

    // Ciocnirea a două expediții: cine trimite mai mult pierde o parte mai mică din
    // ce a trimis. Moralul mai mare valorează trupe în plus pe câmp.
    public const double ClashBaseLossFraction = 0.30;
    public const double ClashLossSwing = 0.70;
    public const int MutualClashMaxMoraleShiftFactor = 5;
    public const int MutualClashMoralePerShiftPoint = 10;
    public const int MutualClashMoraleLoss = 5;

    // Prada la angajament total. Sub el se ia proporțional cu cât s-a trimis.
    public const int BreakthroughStealGold = 12;
    public const int BreakthroughStealTroops = 8;
    public const int UndefendedStealGold = 15;
    public const int UndefendedStealTroops = 10;
    public const int UndefendedDiplomacyStealGold = 20;
    public const int UndefendedDiplomacyStealTroops = 15;
    public const int UndefendedAttackerMoraleGain = 5;

    public const int RepelledDefenderMoraleBase = 5;
    public const int ChasedOffDefenderMoraleGain = 2;

    // Cât din expediție nu se mai întoarce. Un asalt frânt de ziduri se pierde
    // întreg — de aici vine riscul de a trimite toată oastea. Fracțiile sunt
    // calibrate ca un atac reușit la angajament total să iasă aproape neutru în
    // trupe, cum era pe vremea costului fix.
    public const double RepelledLossFraction = 1.00;
    public const double RaidChasedOffLossFraction = 0.60;
    public const double BreakthroughLossFraction = 0.45;
    public const double RaidSuccessLossFraction = 0.35;

    // Șansa de a străpunge o fortificație: pornește de la minim și urcă cu cât se
    // trimite mai mult, plus diferența de echipament.
    public const double BreakthroughChancePerLevel = 0.08;
    public const double BreakthroughMinChance = 0.05;
    public const double BreakthroughCommitmentSpan = 0.55;
    public const double BreakthroughMaxChance = 0.80;

    // Un regat care nu se fortifică se apără totuși cu ce are la îndemână: o
    // expediție mică poate fi alungată, una completă trece sigur.
    public const double RaidMinChance = 0.35;
}
