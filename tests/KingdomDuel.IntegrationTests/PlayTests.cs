using KingdomDuel.Domain;
using KingdomDuel.Domain.Enums;
using KingdomDuel.Shared.Dtos.Matches;
using KingdomDuel.Shared.Requests.Matches;

namespace KingdomDuel.IntegrationTests;

public sealed class PlayTests(ApiFactory api)
{
    [Fact]
    public async Task The_round_resolves_once_both_have_ordered()
    {
        var duel = await Duel.StartAsync(api);

        var waiting = await duel.OrderAsync(duel.Host, new SubmitOrderRequest { Feast = FeastTier.Small });
        waiting.Round.ShouldBe(1);

        var next = await duel.OrderAsync(duel.Guest);

        next.Round.ShouldBe(2);
        var recap = (await duel.StateAsync(duel.Host)).LastRound!;
        recap.Round.ShouldBe(1);
        recap.GoldDelta.ShouldBe(-GameConstants.FeastSmallCost);
    }

    [Fact]
    public async Task An_order_beyond_the_kingdom_is_refused_with_the_reason()
    {
        var duel = await Duel.StartAsync(api);

        var error = await duel.Host.FailAsync(HttpMethod.Post, $"{duel.Path}/orders", Duel.Attack(11));

        error.Messages.ShouldContain("Ai trimis 11 soldați, dar oastea are 10.");
    }

    [Fact]
    public async Task A_malformed_order_is_refused()
    {
        var duel = await Duel.StartAsync(api);

        var order = new SubmitOrderRequest
        {
            Troops =
            [
                new OrderAllocationDto { Direction = ActionType.Attack, Troops = 2 },
                new OrderAllocationDto { Direction = ActionType.Attack, Troops = 3 },
            ],
        };

        (await duel.Host.FailAsync(HttpMethod.Post, $"{duel.Path}/orders", order)).Messages
            .ShouldContain("Aceeași direcție apare de două ori.");
    }

    [Fact]
    public async Task The_other_kingdom_stays_hidden_over_the_wire()
    {
        var duel = await Duel.StartAsync(api);

        var seen = await duel.StateAsync(duel.Guest);

        seen.Player1Kingdom.ShouldBeNull();
        seen.Player2Kingdom.ShouldNotBeNull();
    }

    [Fact]
    public async Task A_match_played_to_the_last_round_ends_and_is_remembered()
    {
        var duel = await Duel.StartAsync(api, Duel.Lobby(totalRounds: GameConstants.MinTotalRounds));

        MatchStateDto state = null!;
        for (var round = 0; round < GameConstants.MinTotalRounds; round++)
        {
            state = await duel.PlayQuietRoundAsync();
        }

        state.Status.ShouldBe(MatchStatus.Completed);
        state.GameOverReason.ShouldBe(GameOverReason.RoundsExhaustedDraw);
        state.Player1Kingdom.ShouldNotBeNull();
        state.Story!.Rounds.Count.ShouldBe(GameConstants.MinTotalRounds + 1);

        foreach (var player in new[] { duel.Host, duel.Guest })
        {
            var history = await player.GetAsync<MatchHistoryDto>("/api/auth/profile/history");

            history.Played.ShouldBe(1);
            history.Draw.ShouldBe(1);
            history.Entries.Single().RoundsPlayed.ShouldBe(GameConstants.MinTotalRounds);
        }
    }

    [Fact]
    public async Task A_silent_player_misses_rounds_and_forfeits_after_three()
    {
        var duel = await Duel.StartAsync(api);

        for (var missed = 1; missed <= GameConstants.MissedRoundsBeforeForfeit; missed++)
        {
            await duel.OrderAsync(duel.Host);
            api.Wait(GameConstants.DefaultRoundSeconds + GameConstants.RoundGraceSeconds + 1);

            var state = await duel.StateAsync(duel.Host);

            state.Player2MissedRounds.ShouldBe(missed);
        }

        var end = await duel.StateAsync(duel.Guest);
        end.Status.ShouldBe(MatchStatus.Completed);
        end.GameOverReason.ShouldBe(GameOverReason.Player2Disconnected);

        (await duel.Host.GetAsync<MatchHistoryDto>("/api/auth/profile/history")).Won.ShouldBe(1);
        (await duel.Guest.GetAsync<MatchHistoryDto>("/api/auth/profile/history")).Lost.ShouldBe(1);
    }

    [Fact]
    public async Task Leaving_a_match_in_play_loses_it()
    {
        var duel = await Duel.StartAsync(api);

        using (await duel.Guest.SendRawAsync(HttpMethod.Post, $"{duel.Path}/leave"))
        {
        }

        var state = await duel.StateAsync(duel.Host);
        state.Status.ShouldBe(MatchStatus.Completed);
        state.GameOverReason.ShouldBe(GameOverReason.Player2Left);
        (await duel.Guest.GetAsync<MatchHistoryDto>("/api/auth/profile/history")).Lost.ShouldBe(1);
    }

    [Fact]
    public async Task An_assault_plays_out_in_time_and_then_the_game_moves_on()
    {
        var duel = await Duel.StartAsync(api);

        await duel.OrderAsync(duel.Host, Duel.Attack(10));
        var battle = await duel.OrderAsync(duel.Guest);

        battle.Battle.ShouldNotBeNull();
        battle.Battle.AttackerSide.ShouldBe(1);
        battle.SecondsRemaining.ShouldBeNull();

        (await duel.Host.FailAsync(HttpMethod.Post, $"{duel.Path}/orders", new SubmitOrderRequest())).Messages
            .ShouldContain(m => m.StartsWith("Bătălia e în desfășurare"));
        (await duel.Guest.FailAsync(HttpMethod.Post, $"{duel.Path}/retreat")).Messages
            .ShouldContain(m => m.StartsWith("Cetatea atacată nu se poate retrage"));

        var after = await duel.SettleAsync(duel.Host);

        after.Battle.ShouldBeNull();
        (after.Round == 2 || after.Status == MatchStatus.Completed).ShouldBeTrue();
    }

    [Fact]
    public async Task A_retreat_is_signalled_once_and_the_other_army_chooses_the_march()
    {
        var duel = await Duel.StartAsync(api);

        await duel.OrderAsync(duel.Host, Duel.Attack(5));
        await duel.OrderAsync(duel.Guest, Duel.Attack(5));

        var retreating = await duel.Host.PostAsync<MatchStateDto>($"{duel.Path}/retreat");
        retreating.Battle!.RetreatBySide.ShouldBe(1);
        (await duel.Host.FailAsync(HttpMethod.Post, $"{duel.Path}/retreat")).Messages
            .ShouldContain("Semnalul de retragere a fost deja dat.");

        api.Wait(GameConstants.RetreatSeconds + 0.1);

        var choosing = await duel.StateAsync(duel.Guest);
        choosing.March.ShouldNotBeNull();
        choosing.March.IsYours.ShouldBeTrue();

        (await duel.Host.FailAsync(HttpMethod.Post, $"{duel.Path}/march", new MarchRequest { Advance = true })).Messages
            .ShouldContain("Alegerea e a celui rămas în picioare pe câmp.");

        var home = await duel.Guest.PostAsync<MatchStateDto>($"{duel.Path}/march", new MarchRequest { Advance = false });
        home.March.ShouldBeNull();
        home.Round.ShouldBe(2);
    }

    [Fact]
    public async Task The_bell_is_answered_by_the_defender_alone()
    {
        var duel = await Duel.StartAsync(api);

        await duel.OrderAsync(duel.Host, Duel.Attack(10));
        await duel.OrderAsync(duel.Guest);
        api.Wait(GameConstants.WallBreakingSeconds + 0.1);

        var defender = await duel.StateAsync(duel.Guest);
        defender.Breach!.IsYours.ShouldBeTrue();
        defender.Breach.Most.ShouldBe(GameConstants.StartingVillagers);

        var attacker = await duel.StateAsync(duel.Host);
        attacker.Breach!.IsYours.ShouldBeFalse();
        attacker.Breach.Most.ShouldBe(0);

        (await duel.Host.FailAsync(HttpMethod.Post, $"{duel.Path}/rally", new RallyRequest { Villagers = 5 })).Messages
            .ShouldContain("Numai cetatea spartă își cheamă satul.");

        var rallied = await duel.Guest.PostAsync<MatchStateDto>($"{duel.Path}/rally", new RallyRequest { Villagers = 5 });
        rallied.Breach.ShouldBeNull();

        var after = await duel.SettleAsync(duel.Guest);
        after.Player2Kingdom!.Villagers.ShouldBe(GameConstants.StartingVillagers - 5);
    }
}
