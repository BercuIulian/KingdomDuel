using System.Net;
using System.Text;
using System.Text.Json;
using Bunit;
using KingdomDuel.Client.ApiClients;
using KingdomDuel.Client.Components;
using KingdomDuel.Domain.Enums;
using KingdomDuel.Shared.Dtos.Matches;
using KingdomDuel.Shared.Requests.Matches;
using Microsoft.Extensions.DependencyInjection;

namespace KingdomDuel.Client.Tests.Components;

public sealed class OrderPanelTests : BunitContext
{
    private readonly FakeServer server = new();

    public OrderPanelTests()
    {
        Services.AddSingleton(new ApiClient(new HttpClient(server) { BaseAddress = new Uri("http://localhost/") })
        {
            ReadResponseAsString = true,
        });
    }

    [Fact]
    public void The_budget_starts_with_the_whole_kingdom_free()
    {
        var panel = Render<OrderPanel>(p => p.Add(x => x.State, State(round: 1)));

        var budget = panel.FindAll(".budget-item b").Select(b => b.TextContent).ToList();

        budget.ShouldBe(["10", "15", "50"]);
    }

    [Fact]
    public void Trade_is_blocked_while_a_caravan_is_on_the_road()
    {
        var state = State(round: 1);
        state.YourCaravan = new CaravanDto { Troops = 5, ReturnsInRound = 3 };

        var panel = Render<OrderPanel>(p => p.Add(x => x.State, state));

        panel.FindAll("button.order")[2].HasAttribute("disabled").ShouldBeTrue();
        panel.Markup.ShouldContain("caravana e pe drum cu 5");
    }

    [Fact]
    public void A_feast_is_sent_with_the_order_and_the_panel_locks()
    {
        MatchStateDto? submitted = null;
        server.Respond(HttpStatusCode.OK, State(round: 1));

        var panel = Render<OrderPanel>(p => p
            .Add(x => x.State, State(round: 1))
            .Add(x => x.OnSubmitted, state => submitted = state));

        panel.FindAll("button.order")[3].Click();
        panel.Find("button.choice").Click();
        panel.FindAll(".result-actions button.send").Single().Click();
        panel.Find(".order-action button.send").Click();

        panel.WaitForAssertion(() => panel.Markup.ShouldContain("Ordin trimis"));
        submitted.ShouldNotBeNull();

        var request = JsonSerializer.Deserialize<SubmitOrderRequest>(server.LastBody!, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        request.Feast.ShouldBe(FeastTier.Small);
        server.LastPath.ShouldBe("/api/matches/DUEL42/orders");
    }

    [Fact]
    public void A_refused_order_shows_the_reason_from_the_server()
    {
        server.Respond(HttpStatusCode.BadRequest, new { statusCode = 400, errors = new { generalErrors = new[] { "Meciul nu este în desfășurare." } } });

        var panel = Render<OrderPanel>(p => p.Add(x => x.State, State(round: 1)));

        panel.Find(".order-action button.send").Click();

        panel.WaitForAssertion(() => panel.Find(".send-error").TextContent.ShouldBe("Meciul nu este în desfășurare."));
    }

    [Fact]
    public void A_new_round_brings_a_blank_order()
    {
        server.Respond(HttpStatusCode.OK, State(round: 1));
        var panel = Render<OrderPanel>(p => p.Add(x => x.State, State(round: 1)));
        panel.Find(".order-action button.send").Click();
        panel.WaitForAssertion(() => panel.Markup.ShouldContain("Ordin trimis"));

        panel.Render(p => p.Add(x => x.State, State(round: 2)));

        panel.Markup.ShouldNotContain("Ordin trimis");
        panel.Find(".order-action button.send").HasAttribute("disabled").ShouldBeFalse();
    }

    private static MatchStateDto State(int round) => new()
    {
        Code = "DUEL42",
        Round = round,
        YourSide = 1,
        Status = MatchStatus.InProgress,
        Player1Kingdom = new KingdomDto { Gold = 50, Troops = 10, Villagers = 15, Morale = 50 },
    };

    private sealed class FakeServer : HttpMessageHandler
    {
        private HttpStatusCode status = HttpStatusCode.OK;
        private string body = "{}";

        public string? LastBody { get; private set; }

        public string? LastPath { get; private set; }

        public void Respond(HttpStatusCode code, object payload)
        {
            status = code;
            body = JsonSerializer.Serialize(payload);
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            LastPath = request.RequestUri!.AbsolutePath;
            LastBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);

            return new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
        }
    }
}
