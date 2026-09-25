using KingdomDuel.Domain.Enums;

namespace KingdomDuel.Domain;

public static class GameOutcome
{
    public static int WinnerSide(GameOverReason reason) => reason switch
    {
        GameOverReason.Player2CastleDestroyed
            or GameOverReason.RoundsExhaustedPlayer1Wins
            or GameOverReason.Player2Left
            or GameOverReason.Player2Disconnected => 1,

        GameOverReason.Player1CastleDestroyed
            or GameOverReason.RoundsExhaustedPlayer2Wins
            or GameOverReason.Player1Left
            or GameOverReason.Player1Disconnected => 2,

        _ => 0,
    };

    public static MatchOutcome OutcomeFor(GameOverReason reason, int side)
    {
        var winner = WinnerSide(reason);

        if (winner == 0)
        {
            return MatchOutcome.Draw;
        }

        return winner == side ? MatchOutcome.Won : MatchOutcome.Lost;
    }
}
