using KingdomDuel.Client.Components;
using KingdomDuel.Domain;
using KingdomDuel.Domain.Enums;
using KingdomDuel.Domain.Rounds;
using KingdomDuel.Shared.Dtos.Matches;

namespace KingdomDuel.Client.Tests.Components;

public sealed class OrderDraftTests
{
    [Fact]
    public void The_budget_shows_what_is_left_after_the_order()
    {
        var order = Draft(Kingdom(gold: 50, troops: 10, villagers: 15));

        order.Allocate(ActionType.Attack, 4);
        order.Allocate(ActionType.Trade, 5);
        order.Feast = FeastTier.Small;
        order.Hunt = HuntTier.Scout;

        order.Free.ShouldBe(10 - 4 - 1);
        order.FreeVillagers.ShouldBe(10);
        order.GoldLeft.ShouldBe(50 - 5 - 1);
        order.Fits.ShouldBeTrue();
    }

    [Fact]
    public void Spies_cost_their_upkeep_and_take_only_the_extra_villagers()
    {
        var order = Draft(Kingdom(gold: 50, villagers: 12), spies: SpyTier.Pair);

        order.GoldLeft.ShouldBe(47);
        order.FreeVillagers.ShouldBe(12);

        order.PickSpies(SpyTier.Ring);
        order.GoldLeft.ShouldBe(45);
        order.FreeVillagers.ShouldBe(10);

        order.CallSpiesHome();
        order.GoldLeft.ShouldBe(50);
        order.ToRequest().RecallSpies.ShouldBeTrue();
    }

    [Fact]
    public void An_order_that_spends_more_than_the_treasury_cannot_be_sent()
    {
        var order = Draft(Kingdom(gold: 10));

        order.Feast = FeastTier.Court;

        order.Fits.ShouldBeFalse();
    }

    [Fact]
    public void A_caravan_on_the_road_blocks_trade_and_a_ruined_wall_blocks_defence()
    {
        Draft(Kingdom(), caravan: new CaravanDto { Troops = 5, ReturnsInRound = 5 }).Blocked(ActionType.Trade).ShouldBeTrue();
        Draft(Kingdom(fortification: -1)).Blocked(ActionType.Defend).ShouldBeTrue();
        Draft(Kingdom()).Blocked(ActionType.Attack).ShouldBeFalse();
    }

    [Fact]
    public void Walls_and_catapults_at_their_top_level_cannot_be_bought()
    {
        var order = Draft(Kingdom(gold: 100, fortification: 2, catapults: 1));

        order.Buyable(PurchaseType.Fortify).ShouldBeFalse();
        order.Buyable(PurchaseType.Catapult).ShouldBeTrue();
    }

    [Fact]
    public void Levels_are_bought_back_only_together_with_the_wall_and_one_at_a_time()
    {
        var order = Draft(Kingdom(gold: 100, fortification: -1, ruinedFrom: 2));

        order.Buyable(PurchaseType.RepairToOne).ShouldBeFalse();

        order.Toggle(PurchaseType.Repair);
        order.Toggle(PurchaseType.RepairToOne);
        order.Toggle(PurchaseType.RepairToTwo);
        order.Bought.ShouldBe([PurchaseType.Repair, PurchaseType.RepairToTwo], ignoreOrder: true);

        order.Toggle(PurchaseType.Repair);
        order.Bought.ShouldBeEmpty();
    }

    [Fact]
    public void The_request_carries_every_line_of_the_order()
    {
        var order = Draft(Kingdom(gold: 100, fortification: -1, ruinedFrom: 1), repairTo: 1);

        order.Allocate(ActionType.Attack, 3);
        order.Allocate(ActionType.Defend, 0);
        order.Feast = FeastTier.Court;
        order.Toggle(PurchaseType.Recruit);
        order.Hired = 1;
        order.Armed = 2;
        order.PickSpies(SpyTier.Lone);
        order.PickHunt(HuntTier.Patrol);

        var request = order.ToRequest();

        request.Code.ShouldBe("DUEL42");
        request.Troops.Single().Direction.ShouldBe(ActionType.Attack);
        request.Purchases.ShouldBe([PurchaseType.Recruit, PurchaseType.Repair, PurchaseType.RepairToOne], ignoreOrder: true);
        request.Feast.ShouldBe(FeastTier.Court);
        request.Hired.ShouldBe(1);
        request.Armed.ShouldBe(2);
        request.Spies.ShouldBe(SpyTier.Lone);
        request.Hunt.ShouldBe(HuntTier.Patrol);
    }

    [Fact]
    public void Choosing_the_same_hunt_again_calls_it_off()
    {
        var order = Draft(Kingdom());

        order.PickHunt(HuntTier.Scout);
        order.PickHunt(HuntTier.Scout);

        order.Hunt.ShouldBeNull();
    }

    [Fact]
    public void The_feast_promised_on_screen_is_the_feast_the_engine_serves()
    {
        for (var seed = 0; seed < 3_000; seed++)
        {
            var random = new Random(seed);
            var order = RandomDraft(random);

            if (order.Feast is not { } tier || !order.Fits)
            {
                continue;
            }

            var promised = order.MoraleFrom(tier);
            var served = Resolve(order).Events
                .Where(e => e.Kind == RoundEventKind.Feast && e.Side == 1)
                .Sum(e => e.Amount);

            served.ShouldBe(promised, $"seed {seed}");
        }
    }

    [Fact]
    public void An_order_the_panel_lets_through_reaches_the_engine_uncut()
    {
        for (var seed = 0; seed < 3_000; seed++)
        {
            var order = RandomDraft(new Random(seed));

            if (!order.Fits)
            {
                continue;
            }

            var plan = Plan(order);
            var clamped = plan.ClampTo(Kingdom(order.Mine!), order.SpiesOut is { } ring ? new SpyRing(ring.Tier) : null);

            foreach (var direction in Enum.GetValues<ActionType>())
            {
                clamped[direction].ShouldBe(plan[direction], $"seed {seed}: {direction}");
            }

            clamped.Feast.ShouldBe(plan.Feast, $"seed {seed}");
            clamped.Purchases.ShouldBe(plan.Purchases, ignoreOrder: true, $"seed {seed}");
            clamped.Hired.ShouldBe(plan.Hired, $"seed {seed}");
            clamped.Armed.ShouldBe(plan.Armed, $"seed {seed}");
            clamped.Spies.ShouldBe(plan.Spies, $"seed {seed}");
            clamped.Hunt.ShouldBe(plan.Hunt, $"seed {seed}");
        }
    }

    // Builds an order the way a player can through the panel: every slider
    // stops at its ceiling, every disabled button stays unpressed.
    internal static OrderDraft RandomDraft(Random random)
    {
        var fortification = random.Next(-1, 3);
        var kingdom = Kingdom(
            gold: random.Next(0, 80),
            troops: random.Next(0, 20),
            morale: random.Next(0, 101),
            villagers: random.Next(0, 20),
            fortification: fortification,
            catapults: random.Next(0, 3),
            ruinedFrom: fortification < 0 ? random.Next(0, 3) : 0);

        var caravan = random.Next(3) == 0
            ? new CaravanDto { Troops = random.Next(1, 10), ReturnsInRound = 3 + random.Next(0, 3) }
            : null;
        SpyTier? spiesOut = random.Next(4) == 0 ? (SpyTier)random.Next(3) : null;
        var repairTo = fortification < 0 && random.Next(2) == 0 ? random.Next(0, kingdom.RuinedFrom + 1) : -1;

        var order = Draft(kingdom, caravan, spiesOut, repairTo);

        if (repairTo >= 0 && order.GoldLeft < 0)
        {
            return order;
        }

        foreach (var direction in Enum.GetValues<ActionType>())
        {
            if (!order.Blocked(direction) && random.Next(2) == 0)
            {
                order.Allocate(direction, random.Next(0, order.Ceiling(direction) + 1));
            }
        }

        if (random.Next(2) == 0)
        {
            var tier = (FeastTier)random.Next(3);

            if (order.Affordable(tier))
            {
                order.Feast = tier;
            }
        }

        foreach (var purchase in new[] { PurchaseType.Recruit, PurchaseType.Fortify, PurchaseType.Catapult })
        {
            if (random.Next(3) == 0 && order.Buyable(purchase) && !(purchase == PurchaseType.Fortify && order.WallDown))
            {
                order.Toggle(purchase);
            }
        }

        if (random.Next(3) == 0)
        {
            order.Hired = random.Next(0, order.MaxBundles + 1);
        }

        if (random.Next(3) == 0 && order.Villagers > 0)
        {
            order.Armed = random.Next(0, order.MaxArmed + 1);
        }

        switch (random.Next(5))
        {
            case 0:
                var tier = (SpyTier)random.Next(3);

                if (order.ChosenSpies == tier || order.CanSendSpies(tier))
                {
                    order.PickSpies(tier);
                }

                break;
            case 1 when spiesOut is not null:
                order.CallSpiesHome();
                break;
        }

        if (random.Next(3) == 0)
        {
            var hunt = (HuntTier)random.Next(3);

            if (GameConstants.HuntSoldiers(hunt) <= order.FreeForHunt(hunt))
            {
                order.PickHunt(hunt);
            }
        }

        return order;
    }

    internal static OrderPlan Plan(OrderDraft order)
    {
        var request = order.ToRequest();

        return new OrderPlan(
            request.Troops.ToDictionary(a => a.Direction, a => a.Troops),
            request.Feast,
            request.Purchases,
            request.Hired,
            request.Armed,
            request.Spies,
            request.RecallSpies,
            request.Hunt);
    }

    internal static RoundOutcome Resolve(OrderDraft order)
    {
        var mine = new SideInput(
            Kingdom(order.Mine!),
            Plan(order),
            order.Caravan is { } caravan ? new Caravan(caravan.Troops, caravan.ReturnsInRound) : null,
            order.SpiesOut is { } ring ? new SpyRing(ring.Tier) : null);

        var other = new SideInput(KingdomState.Start(50, 10, 50, 15), OrderPlan.Empty);

        return GameEngine.Resolve(new RoundInput(order.State.Round, 10, mine, other), new Random(1));
    }

    private static KingdomState Kingdom(KingdomDto dto) =>
        new(dto.Gold, dto.Troops, dto.Morale, dto.Villagers, dto.Fortification, dto.Catapults, dto.RuinedFrom);

    private static KingdomDto Kingdom(
        int gold = 50,
        int troops = 10,
        int morale = 50,
        int villagers = 15,
        int fortification = 0,
        int catapults = 0,
        int ruinedFrom = 0) => new()
    {
        Gold = gold,
        Troops = troops,
        Morale = morale,
        Villagers = villagers,
        Fortification = fortification,
        Catapults = catapults,
        RuinedFrom = ruinedFrom,
    };

    private static OrderDraft Draft(
        KingdomDto kingdom,
        CaravanDto? caravan = null,
        SpyTier? spies = null,
        int repairTo = -1) => new(
        new MatchStateDto
        {
            Code = "DUEL42",
            Round = 3,
            YourSide = 1,
            Player1Kingdom = kingdom,
            YourCaravan = caravan,
            YourSpies = spies is { } tier
                ? new SpyRingDto { Tier = tier, Villagers = GameConstants.SpyVillagers(tier), Upkeep = GameConstants.SpyUpkeep(tier) }
                : null,
        },
        repairTo);
}
