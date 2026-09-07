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

    public const int TradeGoldPerSoldier = 1;
    public const double TradeYieldMin = 0.9;
    public const double TradeYieldMax = 1.1;
}
