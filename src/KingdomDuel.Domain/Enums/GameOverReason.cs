namespace KingdomDuel.Domain.Enums;

public enum GameOverReason
{
    None,
    Player1Collapsed,
    Player2Collapsed,
    BothCollapsed,
    RoundsExhaustedPlayer1Wins,
    RoundsExhaustedPlayer2Wins,
    RoundsExhaustedDraw
}
