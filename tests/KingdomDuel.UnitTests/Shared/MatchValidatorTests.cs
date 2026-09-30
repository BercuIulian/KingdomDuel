using FluentValidation.TestHelper;
using KingdomDuel.Domain;
using KingdomDuel.Domain.Enums;
using KingdomDuel.Shared.Dtos.Matches;
using KingdomDuel.Shared.Requests.Matches;
using KingdomDuel.Shared.Validators.Matches;

namespace KingdomDuel.UnitTests.Shared;

public sealed class MatchValidatorTests
{
    private readonly SubmitOrderValidator order = new();
    private readonly UpdateLobbyValidator lobby = new();

    [Fact]
    public void An_empty_order_is_valid()
    {
        order.TestValidate(new SubmitOrderRequest()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void A_direction_with_no_soldiers_is_refused()
    {
        order.TestValidate(Order((ActionType.Attack, 0))).ShouldHaveValidationErrorFor("Troops[0].Troops");
    }

    [Fact]
    public void An_unknown_direction_is_refused()
    {
        order.TestValidate(Order(((ActionType)9, 3))).ShouldHaveValidationErrorFor("Troops[0].Direction");
    }

    [Fact]
    public void A_direction_given_twice_is_refused()
    {
        order.TestValidate(Order((ActionType.Attack, 3), (ActionType.Attack, 2))).ShouldHaveValidationErrorFor(r => r.Troops);
    }

    [Fact]
    public void An_unknown_feast_is_refused()
    {
        order.TestValidate(new SubmitOrderRequest { Feast = (FeastTier)9 }).ShouldHaveValidationErrorFor(r => r.Feast);
    }

    [Fact]
    public void The_default_lobby_is_valid()
    {
        lobby.TestValidate(Lobby()).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(GameConstants.MinTotalRounds - 1, false)]
    [InlineData(GameConstants.MinTotalRounds, true)]
    [InlineData(GameConstants.MaxTotalRounds, true)]
    [InlineData(GameConstants.MaxTotalRounds + 1, false)]
    public void Rounds_stay_between_eight_and_thirty(int rounds, bool valid)
    {
        Check(lobby.TestValidate(Lobby(totalRounds: rounds)), r => r.TotalRounds, valid);
    }

    [Theory]
    [InlineData(GameConstants.MinRoundSeconds - 1, false)]
    [InlineData(GameConstants.MinRoundSeconds, true)]
    [InlineData(GameConstants.MaxRoundSeconds, true)]
    [InlineData(GameConstants.MaxRoundSeconds + 1, false)]
    public void A_round_lasts_between_fifteen_and_one_hundred_eighty_seconds(int seconds, bool valid)
    {
        Check(lobby.TestValidate(Lobby(roundSeconds: seconds)), r => r.RoundSeconds, valid);
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(500, true)]
    [InlineData(501, false)]
    public void Starting_gold_may_be_zero(int gold, bool valid)
    {
        Check(lobby.TestValidate(Lobby(gold: gold)), r => r.StartingGold, valid);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(500, true)]
    [InlineData(501, false)]
    public void Starting_soldiers_and_villagers_need_at_least_one(int count, bool valid)
    {
        Check(lobby.TestValidate(Lobby(troops: count)), r => r.StartingTroops, valid);
        Check(lobby.TestValidate(Lobby(villagers: count)), r => r.StartingVillagers, valid);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(100, true)]
    [InlineData(101, false)]
    public void Starting_morale_stays_under_the_cap(int morale, bool valid)
    {
        Check(lobby.TestValidate(Lobby(morale: morale)), r => r.StartingMorale, valid);
    }

    private static void Check<T>(
        TestValidationResult<UpdateLobbyRequest> result,
        System.Linq.Expressions.Expression<Func<UpdateLobbyRequest, T>> property,
        bool valid)
    {
        if (valid)
        {
            result.ShouldNotHaveValidationErrorFor(property);
        }
        else
        {
            result.ShouldHaveValidationErrorFor(property);
        }
    }

    private static SubmitOrderRequest Order(params (ActionType Direction, int Troops)[] troops) => new()
    {
        Troops = troops.Select(t => new OrderAllocationDto { Direction = t.Direction, Troops = t.Troops }).ToList(),
    };

    private static UpdateLobbyRequest Lobby(
        int totalRounds = GameConstants.DefaultTotalRounds,
        int gold = GameConstants.StartingGold,
        int troops = GameConstants.StartingSoldiers,
        int villagers = GameConstants.StartingVillagers,
        int morale = GameConstants.StartingResource,
        int roundSeconds = GameConstants.DefaultRoundSeconds) => new()
    {
        TotalRounds = totalRounds,
        StartingGold = gold,
        StartingTroops = troops,
        StartingVillagers = villagers,
        StartingMorale = morale,
        RoundSeconds = roundSeconds,
    };
}
