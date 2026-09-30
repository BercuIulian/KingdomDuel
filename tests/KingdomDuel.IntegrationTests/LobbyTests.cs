using System.Net;
using KingdomDuel.Domain.Enums;
using KingdomDuel.Shared.Dtos.Matches;
using KingdomDuel.Shared.Responses.Matches;

namespace KingdomDuel.IntegrationTests;

public sealed class LobbyTests(ApiFactory api)
{
    [Fact]
    public async Task The_host_opens_a_lobby_and_gets_it_back_while_it_waits()
    {
        var host = await api.NewPlayer().RegisterAsync();

        var created = await host.PostAsync<CreateMatchResponse>("/api/matches");
        var again = await host.PostAsync<CreateMatchResponse>("/api/matches");

        created.MatchCode.Length.ShouldBe(6);
        created.State.Status.ShouldBe(MatchStatus.WaitingForPlayer);
        created.State.IsHost.ShouldBeTrue();
        again.MatchCode.ShouldBe(created.MatchCode);
    }

    [Fact]
    public async Task A_guest_joins_by_code_in_any_case()
    {
        var host = await api.NewPlayer().RegisterAsync();
        var guest = await api.NewPlayer().RegisterAsync();
        var created = await host.PostAsync<CreateMatchResponse>("/api/matches");

        var joined = await guest.PostAsync<JoinMatchResponse>($"/api/matches/{created.MatchCode.ToLowerInvariant()}/join");
        var rejoined = await guest.PostAsync<JoinMatchResponse>($"/api/matches/{created.MatchCode}/join");

        joined.State.YourSide.ShouldBe(2);
        rejoined.State.YourSide.ShouldBe(2);
        (await host.GetAsync<MatchStateDto>($"/api/matches/{created.MatchCode}")).Player2Name.ShouldBe(guest.UserName);
    }

    [Fact]
    public async Task A_third_player_finds_the_table_full()
    {
        var duel = await Duel.OpenAsync(api);
        var third = await api.NewPlayer().RegisterAsync();

        var error = await third.FailAsync(HttpMethod.Post, $"{duel.Path}/join");

        error.Messages.ShouldContain("Meciul are deja doi jucători.");
    }

    [Fact]
    public async Task Strangers_and_unknown_codes_find_nothing()
    {
        var duel = await Duel.OpenAsync(api);
        var stranger = await api.NewPlayer().RegisterAsync();

        await stranger.FailAsync(HttpMethod.Get, duel.Path, expected: HttpStatusCode.NotFound);
        await stranger.FailAsync(HttpMethod.Get, "/api/matches/ZZZZZZ", expected: HttpStatusCode.NotFound);
        await stranger.FailAsync(HttpMethod.Post, "/api/matches/ZZZZZZ/join", expected: HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Only_the_host_sets_the_rules_and_only_the_guest_says_ready()
    {
        var duel = await Duel.OpenAsync(api);

        await duel.Host.PutAsync<MatchStateDto>($"{duel.Path}/lobby", Duel.Lobby(totalRounds: 12, gold: 80));
        var afterGuest = await duel.Guest.PutAsync<MatchStateDto>(
            $"{duel.Path}/lobby",
            Duel.Lobby(totalRounds: 30, gold: 500, ready: true));

        afterGuest.TotalRounds.ShouldBe(12);
        afterGuest.StartingGold.ShouldBe(80);
        afterGuest.Player2Ready.ShouldBeTrue();
    }

    [Fact]
    public async Task Lobby_settings_outside_the_limits_are_refused()
    {
        var duel = await Duel.OpenAsync(api);

        var error = await duel.Host.FailAsync(HttpMethod.Put, $"{duel.Path}/lobby", Duel.Lobby(totalRounds: 31));

        error.Messages.ShouldContain(m => m.Contains("Numărul de runde"));
    }

    [Fact]
    public async Task The_match_starts_only_by_the_host_and_only_when_the_guest_is_ready()
    {
        var duel = await Duel.OpenAsync(api);

        (await duel.Guest.FailAsync(HttpMethod.Post, $"{duel.Path}/start")).Messages
            .ShouldContain("Doar gazda poate porni meciul.");
        (await duel.Host.FailAsync(HttpMethod.Post, $"{duel.Path}/start")).Messages
            .ShouldContain("Jucătorul 2 nu este pregătit.");

        var started = await duel.StartAsync(Duel.Lobby(gold: 80, roundSeconds: 30));

        started.Status.ShouldBe(MatchStatus.InProgress);
        started.Round.ShouldBe(1);
        started.SecondsRemaining.ShouldBe(30);
        started.Player1Kingdom!.Gold.ShouldBe(80);
    }

    [Fact]
    public async Task A_player_in_a_match_cannot_open_or_join_another()
    {
        var duel = await Duel.StartAsync(api);
        var other = await Duel.OpenAsync(api);

        (await duel.Host.FailAsync(HttpMethod.Post, "/api/matches")).Messages
            .ShouldContain(m => m.Contains(duel.Code));
        (await duel.Guest.FailAsync(HttpMethod.Post, $"{other.Path}/join")).Messages
            .ShouldContain(m => m.Contains(duel.Code));

        var mine = await duel.Host.GetAsync<ActiveMatchResponse>("/api/matches/mine");
        mine.Match!.Code.ShouldBe(duel.Code);
    }

    [Fact]
    public async Task A_host_leaving_the_lobby_closes_it()
    {
        var duel = await Duel.OpenAsync(api);

        using (await duel.Host.SendRawAsync(HttpMethod.Post, $"{duel.Path}/leave"))
        {
        }

        await duel.Guest.FailAsync(HttpMethod.Get, duel.Path, expected: HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task A_guest_leaving_the_lobby_frees_the_seat()
    {
        var duel = await Duel.OpenAsync(api);

        using (await duel.Guest.SendRawAsync(HttpMethod.Post, $"{duel.Path}/leave"))
        {
        }

        (await duel.Host.GetAsync<MatchStateDto>(duel.Path)).Player2Name.ShouldBeNull();
        var newcomer = await api.NewPlayer().RegisterAsync();
        (await newcomer.PostAsync<JoinMatchResponse>($"{duel.Path}/join")).State.YourSide.ShouldBe(2);
    }
}
