using KingdomDuel.Domain;
using KingdomDuel.Domain.Enums;
using KingdomDuel.Domain.Rounds;
using static KingdomDuel.UnitTests.Domain.GameSetup;

namespace KingdomDuel.UnitTests.Domain;

public sealed class MatchEndTests
{
    [Fact]
    public void Morale_drops_by_three_every_round()
    {
        var outcome = Resolve(Side(Kingdom(morale: 50)));

        outcome.Player1.Kingdom.Morale.ShouldBe(47);
        outcome.Count(RoundEventKind.MoraleDecay, 1).ShouldBe(3);
    }

    [Fact]
    public void Morale_stops_at_zero()
    {
        Resolve(Side(Kingdom(morale: 2))).Player1.Kingdom.Morale.ShouldBe(0);
    }

    [Fact]
    public void The_match_goes_on_before_the_last_round()
    {
        Resolve(round: 9, totalRounds: 10).GameOverReason.ShouldBe(GameOverReason.None);
    }

    [Theory]
    [InlineData(60, 50, GameOverReason.RoundsExhaustedPlayer1Wins)]
    [InlineData(50, 60, GameOverReason.RoundsExhaustedPlayer2Wins)]
    [InlineData(50, 50, GameOverReason.RoundsExhaustedDraw)]
    public void After_the_last_round_the_greater_power_wins(int gold1, int gold2, GameOverReason reason)
    {
        var outcome = Resolve(Side(Kingdom(gold: gold1)), Side(Kingdom(gold: gold2)), round: 10, totalRounds: 10);

        outcome.GameOverReason.ShouldBe(reason);
    }

    [Fact]
    public void Traders_and_spies_away_count_at_the_final_reckoning()
    {
        var outcome = Resolve(
            Side(Kingdom(gold: 50, villagers: 5), caravan: new Caravan(10, 12)),
            Side(Kingdom(gold: 50, villagers: 14)),
            round: 10,
            totalRounds: 10);

        outcome.GameOverReason.ShouldBe(GameOverReason.RoundsExhaustedPlayer1Wins);
    }

    [Theory]
    [InlineData(GameOverReason.Player2CastleDestroyed, 1)]
    [InlineData(GameOverReason.RoundsExhaustedPlayer1Wins, 1)]
    [InlineData(GameOverReason.Player2Left, 1)]
    [InlineData(GameOverReason.Player2Disconnected, 1)]
    [InlineData(GameOverReason.Player1CastleDestroyed, 2)]
    [InlineData(GameOverReason.RoundsExhaustedPlayer2Wins, 2)]
    [InlineData(GameOverReason.Player1Left, 2)]
    [InlineData(GameOverReason.Player1Disconnected, 2)]
    [InlineData(GameOverReason.RoundsExhaustedDraw, 0)]
    [InlineData(GameOverReason.BothDisconnected, 0)]
    [InlineData(GameOverReason.None, 0)]
    public void Every_ending_names_its_winner(GameOverReason reason, int winner)
    {
        GameOutcome.WinnerSide(reason).ShouldBe(winner);
    }

    [Theory]
    [InlineData(GameOverReason.Player2Left, 1, MatchOutcome.Won)]
    [InlineData(GameOverReason.Player2Left, 2, MatchOutcome.Lost)]
    [InlineData(GameOverReason.RoundsExhaustedDraw, 1, MatchOutcome.Draw)]
    public void Each_side_reads_the_ending_from_its_own_seat(GameOverReason reason, int side, MatchOutcome outcome)
    {
        GameOutcome.OutcomeFor(reason, side).ShouldBe(outcome);
    }
}
