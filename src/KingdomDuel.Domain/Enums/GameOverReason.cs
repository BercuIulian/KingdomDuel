namespace KingdomDuel.Domain.Enums;

public enum GameOverReason
{
    None,
    Player1Collapsed,
    Player2Collapsed,
    BothCollapsed,
    RoundsExhaustedPlayer1Wins,
    RoundsExhaustedPlayer2Wins,
    RoundsExhaustedDraw,

    /// <summary>Abandon în timpul meciului: cel rămas câștigă fără luptă.</summary>
    Player1Left,
    Player2Left
}
