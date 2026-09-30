using KingdomDuel.Domain;
using KingdomDuel.Domain.Enums;
using KingdomDuel.Shared.Dtos.Matches;
using KingdomDuel.Shared.Requests.Matches;
using KingdomDuel.Shared.Responses.Matches;

namespace KingdomDuel.IntegrationTests;

public sealed class Duel
{
    private readonly ApiFactory api;

    private Duel(ApiFactory api, Player host, Player guest, string code)
    {
        this.api = api;
        Host = host;
        Guest = guest;
        Code = code;
    }

    public Player Host { get; }

    public Player Guest { get; }

    public string Code { get; }

    public string Path => $"/api/matches/{Code}";

    public static async Task<Duel> OpenAsync(ApiFactory api)
    {
        var host = await api.NewPlayer().RegisterAsync();
        var guest = await api.NewPlayer().RegisterAsync();

        var created = await host.PostAsync<CreateMatchResponse>("/api/matches");
        await guest.PostAsync<JoinMatchResponse>($"/api/matches/{created.MatchCode}/join");

        return new Duel(api, host, guest, created.MatchCode);
    }

    public static async Task<Duel> StartAsync(ApiFactory api, UpdateLobbyRequest? lobby = null)
    {
        var duel = await OpenAsync(api);
        await duel.StartAsync(lobby);

        return duel;
    }

    public static UpdateLobbyRequest Lobby(
        int totalRounds = GameConstants.DefaultTotalRounds,
        int gold = GameConstants.StartingGold,
        int troops = GameConstants.StartingSoldiers,
        int villagers = GameConstants.StartingVillagers,
        int morale = GameConstants.StartingResource,
        int roundSeconds = GameConstants.DefaultRoundSeconds,
        bool ready = false) => new()
    {
        TotalRounds = totalRounds,
        StartingGold = gold,
        StartingTroops = troops,
        StartingVillagers = villagers,
        StartingMorale = morale,
        RoundSeconds = roundSeconds,
        IsReady = ready,
    };

    public async Task<MatchStateDto> StartAsync(UpdateLobbyRequest? lobby = null)
    {
        await Host.PutAsync<MatchStateDto>($"{Path}/lobby", lobby ?? Lobby());
        await Guest.PutAsync<MatchStateDto>($"{Path}/lobby", Lobby(ready: true));

        return await Host.PostAsync<MatchStateDto>($"{Path}/start");
    }

    public Task<MatchStateDto> StateAsync(Player player) => player.GetAsync<MatchStateDto>(Path);

    public Task<MatchStateDto> OrderAsync(Player player, SubmitOrderRequest? order = null) =>
        player.PostAsync<MatchStateDto>($"{Path}/orders", order ?? new SubmitOrderRequest());

    public async Task<MatchStateDto> PlayQuietRoundAsync()
    {
        await OrderAsync(Host);

        return await OrderAsync(Guest);
    }

    public static SubmitOrderRequest Attack(int troops) => new()
    {
        Troops = [new OrderAllocationDto { Direction = ActionType.Attack, Troops = troops }],
    };

    public async Task<MatchStateDto> SettleAsync(Player watcher)
    {
        for (var step = 0; step < 20; step++)
        {
            var state = await StateAsync(watcher);

            var wait = state switch
            {
                { Breach: { } breach } => breach.SecondsRemaining + 0.1,
                { March: { } march } => march.SecondsRemaining + 0.1,
                { Battle: { } battle } => Math.Max(0.1, battle.Duration - battle.Elapsed + 0.1),
                _ => (double?)null,
            };

            if (wait is null)
            {
                return state;
            }

            api.Wait(wait.Value);
        }

        throw new ShouldAssertException("Bătălia nu s-a încheiat după 20 de pași.");
    }
}
