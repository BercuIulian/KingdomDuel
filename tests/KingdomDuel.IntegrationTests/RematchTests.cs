using System.Net;
using KingdomDuel.Domain;
using KingdomDuel.Domain.Enums;
using KingdomDuel.Shared.Dtos.Matches;
using KingdomDuel.Shared.Responses.Matches;

namespace KingdomDuel.IntegrationTests;

public sealed class RematchTests(ApiFactory api)
{
    private static async Task<Duel> FinishedAsync(ApiFactory api)
    {
        var duel = await Duel.StartAsync(api, Duel.Lobby(totalRounds: GameConstants.MinTotalRounds, gold: 70, roundSeconds: 30));

        for (var round = 0; round < GameConstants.MinTotalRounds; round++)
        {
            await duel.PlayQuietRoundAsync();
        }

        return duel;
    }

    [Fact]
    public async Task A_finished_match_offers_a_rematch_to_both()
    {
        var duel = await FinishedAsync(api);

        var rematch = (await duel.StateAsync(duel.Guest)).Rematch!;

        rematch.Available.ShouldBeTrue();
        rematch.YouAsked.ShouldBeFalse();
        rematch.OpponentAsked.ShouldBeFalse();
        rematch.Code.ShouldBeNull();
    }

    [Fact]
    public async Task One_asking_leaves_the_other_to_answer()
    {
        var duel = await FinishedAsync(api);

        var asked = await duel.Host.PostAsync<MatchStateDto>($"{duel.Path}/rematch");
        var seen = await duel.StateAsync(duel.Guest);

        asked.Rematch!.YouAsked.ShouldBeTrue();
        asked.Rematch.Code.ShouldBeNull();
        seen.Rematch!.OpponentAsked.ShouldBeTrue();
        seen.Rematch.YouAsked.ShouldBeFalse();
    }

    [Fact]
    public async Task Both_asking_sets_a_new_match_with_the_same_rules_after_a_countdown()
    {
        var duel = await FinishedAsync(api);

        await duel.Host.PostAsync<MatchStateDto>($"{duel.Path}/rematch");
        var accepted = await duel.Guest.PostAsync<MatchStateDto>($"{duel.Path}/rematch");
        var hostSees = await duel.StateAsync(duel.Host);

        var code = accepted.Rematch!.Code.ShouldNotBeNull();
        code.ShouldNotBe(duel.Code);
        hostSees.Rematch!.Code.ShouldBe(code);
        accepted.Rematch.SecondsRemaining.ShouldBe(GameConstants.RematchCountdownSeconds);
        accepted.Rematch.Available.ShouldBeFalse();

        var next = await duel.Guest.GetAsync<MatchStateDto>($"/api/matches/{code}");

        next.Status.ShouldBe(MatchStatus.InProgress);
        next.Round.ShouldBe(1);
        next.YourSide.ShouldBe(2);
        next.Player1Name.ShouldBe(duel.Host.UserName);
        next.TotalRounds.ShouldBe(GameConstants.MinTotalRounds);
        next.RoundSeconds.ShouldBe(30);
        next.Player2Kingdom!.Gold.ShouldBe(70);
        next.SecondsRemaining.ShouldBe(30 + GameConstants.RematchCountdownSeconds);

        (await duel.Host.GetAsync<ActiveMatchResponse>("/api/matches/mine")).Match!.Code.ShouldBe(code);
    }

    [Fact]
    public async Task Asking_again_after_the_rematch_is_set_changes_nothing()
    {
        var duel = await FinishedAsync(api);

        await duel.Host.PostAsync<MatchStateDto>($"{duel.Path}/rematch");
        var first = await duel.Guest.PostAsync<MatchStateDto>($"{duel.Path}/rematch");
        var again = await duel.Host.PostAsync<MatchStateDto>($"{duel.Path}/rematch");

        again.Rematch!.Code.ShouldBe(first.Rematch!.Code);
    }

    [Fact]
    public async Task A_rematch_cannot_be_asked_while_the_match_is_on()
    {
        var duel = await Duel.StartAsync(api);

        var error = await duel.Host.FailAsync(HttpMethod.Post, $"{duel.Path}/rematch");

        error.Messages.ShouldContain("Revanșa se cere doar după ce meciul s-a încheiat.");
        (await duel.StateAsync(duel.Host)).Rematch.ShouldBeNull();
    }

    [Fact]
    public async Task There_is_no_rematch_with_someone_who_left_the_table()
    {
        var duel = await FinishedAsync(api);

        using (await duel.Guest.SendRawAsync(HttpMethod.Post, $"{duel.Path}/leave"))
        {
        }

        var seen = await duel.StateAsync(duel.Host);
        var error = await duel.Host.FailAsync(HttpMethod.Post, $"{duel.Path}/rematch");

        seen.Rematch!.Available.ShouldBeFalse();
        seen.Rematch.OpponentGone.ShouldBeTrue();
        error.Messages.ShouldContain("Adversarul a părăsit masa.");
    }

    [Fact]
    public async Task There_is_no_rematch_after_an_abandon()
    {
        var duel = await Duel.StartAsync(api);

        using (await duel.Guest.SendRawAsync(HttpMethod.Post, $"{duel.Path}/leave"))
        {
        }

        var seen = await duel.StateAsync(duel.Host);

        seen.Rematch!.Available.ShouldBeFalse();
        seen.Rematch.OpponentGone.ShouldBeTrue();
        await duel.Host.FailAsync(HttpMethod.Post, $"{duel.Path}/rematch", expected: HttpStatusCode.BadRequest);
    }
}
