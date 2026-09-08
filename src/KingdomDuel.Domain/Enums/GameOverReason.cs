namespace KingdomDuel.Domain.Enums;

public enum GameOverReason
{
    None,
    Player1CastleDestroyed,
    Player2CastleDestroyed,
    RoundsExhaustedPlayer1Wins,
    RoundsExhaustedPlayer2Wins,
    RoundsExhaustedDraw,
    Player1Left,
    Player2Left,
    Player1Disconnected,
    Player2Disconnected,
    BothDisconnected
}
