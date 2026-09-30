using KingdomDuel.Domain.Enums;
using KingdomDuel.Domain.Rounds;
using static KingdomDuel.UnitTests.Domain.GameSetup;

namespace KingdomDuel.UnitTests.Domain;

public sealed class GoldTests
{
    [Theory]
    [InlineData(FeastTier.Small, 5, 4)]
    [InlineData(FeastTier.Court, 12, 10)]
    [InlineData(FeastTier.Grand, 25, 22)]
    public void A_feast_with_everyone_home_gives_its_full_morale(FeastTier tier, int cost, int morale)
    {
        var outcome = Resolve(Side(Kingdom(gold: 50, morale: 50), Order(feast: tier)));

        outcome.Player1.Kingdom.Gold.ShouldBe(50 - cost);
        outcome.Player1.Kingdom.Morale.ShouldBe(50 - 3 + morale);
    }

    [Fact]
    public void Traders_on_the_road_miss_the_feast()
    {
        // home: 10 soldiers + 5 villagers at half = 12.5; away: 10 villagers at half = 5
        var outcome = Resolve(Side(Kingdom(troops: 10, villagers: 15, morale: 50), Order(trade: 10, feast: FeastTier.Grand)));

        outcome.Player1.Kingdom.Morale.ShouldBe(47 + 16);
    }

    [Fact]
    public void Spies_miss_the_feast()
    {
        // home: 10 soldiers + 10 villagers at half = 15; away: 5 spies at half = 2.5
        var outcome = Resolve(Side(
            Kingdom(troops: 10, villagers: 10, morale: 50),
            Order(feast: FeastTier.Grand),
            spies: new SpyRing(SpyTier.Ring)));

        outcome.Player1.Kingdom.Morale.ShouldBe(47 + 19);
    }

    [Fact]
    public void Soldiers_on_the_wall_miss_the_feast()
    {
        // home: 15 villagers at half = 7.5; on the wall: 10 soldiers
        var outcome = Resolve(Side(Kingdom(troops: 10, villagers: 15, morale: 50), Order(defend: 10, feast: FeastTier.Grand)));

        outcome.Player1.Kingdom.Morale.ShouldBe(47 + 9);
    }

    [Fact]
    public void A_villager_weighs_half_a_soldier_at_the_table()
    {
        GameEngine.FeastWeight(10, 10).ShouldBe(15);
    }

    [Fact]
    public void Morale_never_rises_above_one_hundred()
    {
        var outcome = Resolve(Side(Kingdom(morale: 95), Order(feast: FeastTier.Grand)));

        outcome.Player1.Kingdom.Morale.ShouldBe(100);
    }

    [Fact]
    public void A_feast_the_treasury_cannot_pay_is_skipped()
    {
        var outcome = Resolve(Side(Kingdom(gold: 4, morale: 50), Order(feast: FeastTier.Small)));

        outcome.Player1.Kingdom.Gold.ShouldBe(4);
        outcome.Player1.Kingdom.Morale.ShouldBe(47);
    }

    [Fact]
    public void Recruits_arrive_at_the_end_of_the_round()
    {
        var outcome = Resolve(Side(Kingdom(gold: 50, troops: 10), Order(buy: [PurchaseType.Recruit])));

        outcome.Player1.Kingdom.Gold.ShouldBe(38);
        outcome.Player1.Kingdom.Troops.ShouldBe(16);
        outcome.Count(RoundEventKind.Recruited, 1).ShouldBe(6);
    }

    [Fact]
    public void Recruits_do_not_march_in_the_round_they_are_paid_for()
    {
        var outcome = Resolve(
            Side(Kingdom(gold: 50, troops: 10), Order(attack: 16, buy: [PurchaseType.Recruit])),
            Side());

        outcome.Battle!.Player1Start.ShouldBe(10);
        outcome.Player1.Recruits.ShouldBe(6);
    }

    [Fact]
    public void Villagers_are_bought_in_bundles_of_five_and_arrive_at_the_end_of_the_round()
    {
        var outcome = Resolve(Side(Kingdom(gold: 50, villagers: 15), Order(hired: 3)));

        outcome.Player1.Kingdom.Gold.ShouldBe(26);
        outcome.Player1.Kingdom.Villagers.ShouldBe(30);
        outcome.Count(RoundEventKind.VillagersHired, 1).ShouldBe(15);
    }

    [Fact]
    public void Villagers_bought_this_round_cannot_answer_the_bell()
    {
        var outcome = Resolve(
            Side(order: Order(attack: 5)),
            Side(Kingdom(gold: 50, villagers: 0), Order(hired: 2)));

        outcome.Player2.Kingdom.Villagers.ShouldBe(0);
        outcome.Player2.Hired.ShouldBe(10);
        GameEngine.RallyCeiling(outcome.Player2.Kingdom).ShouldBe(0);
    }

    [Fact]
    public void Villagers_bought_before_a_battle_arrive_when_it_is_over()
    {
        var outcome = Play(
            Side(order: Order(attack: 3)),
            Side(Kingdom(gold: 50, villagers: 0), Order(defend: 6, hired: 2)),
            random: new FixedRandom(FixedRandom.FirstLoses));

        outcome.Player2.Kingdom.Villagers.ShouldBe(10);
        outcome.Player2.Hired.ShouldBe(0);
    }

    [Fact]
    public void Only_the_bundles_the_gold_covers_are_bought()
    {
        var outcome = Resolve(Side(Kingdom(gold: 20, villagers: 0), Order(hired: 5)));

        outcome.Player1.Kingdom.Gold.ShouldBe(4);
        outcome.Player1.Kingdom.Villagers.ShouldBe(10);
    }

    [Fact]
    public void Armed_villagers_join_the_army_at_the_end_of_the_round()
    {
        var outcome = Resolve(Side(Kingdom(gold: 50, troops: 10, villagers: 15), Order(armed: 5)));

        outcome.Player1.Kingdom.Gold.ShouldBe(45);
        outcome.Player1.Kingdom.Villagers.ShouldBe(10);
        outcome.Player1.Kingdom.Troops.ShouldBe(15);
    }

    [Fact]
    public void Villagers_bought_this_round_cannot_be_armed_until_tomorrow()
    {
        var outcome = Resolve(Side(Kingdom(gold: 50, troops: 10, villagers: 5), Order(trade: 5, hired: 1, armed: 5)));

        outcome.Player1.Kingdom.Troops.ShouldBe(10);
        outcome.Player1.Kingdom.Villagers.ShouldBe(5);
    }

    [Fact]
    public void Fortifying_raises_the_wall_one_level()
    {
        var outcome = Resolve(Side(Kingdom(gold: 50), Order(buy: [PurchaseType.Fortify])));

        outcome.Player1.Kingdom.Fortification.ShouldBe(1);
        outcome.Player1.Kingdom.Gold.ShouldBe(38);
    }

    [Fact]
    public void The_wall_stops_at_two_fortifications_and_the_gold_is_kept()
    {
        var outcome = Resolve(Side(Kingdom(gold: 50, fortification: 2), Order(buy: [PurchaseType.Fortify])));

        outcome.Player1.Kingdom.Fortification.ShouldBe(2);
        outcome.Player1.Kingdom.Gold.ShouldBe(50);
    }

    [Fact]
    public void A_ruined_wall_cannot_be_fortified()
    {
        var outcome = Resolve(Side(Kingdom(gold: 50, fortification: -1), Order(buy: [PurchaseType.Fortify])));

        outcome.Player1.Kingdom.Fortification.ShouldBe(-1);
        outcome.Player1.Kingdom.Gold.ShouldBe(50);
    }

    [Fact]
    public void Catapults_stop_at_two()
    {
        Resolve(Side(Kingdom(gold: 50), Order(buy: [PurchaseType.Catapult]))).Player1.Kingdom
            .ShouldSatisfyAllConditions(k => k.Catapults.ShouldBe(1), k => k.Gold.ShouldBe(35));

        Resolve(Side(Kingdom(gold: 50, catapults: 2), Order(buy: [PurchaseType.Catapult]))).Player1.Kingdom
            .ShouldSatisfyAllConditions(k => k.Catapults.ShouldBe(2), k => k.Gold.ShouldBe(50));
    }

    [Fact]
    public void A_ruined_wall_is_raised_again_for_twelve_gold()
    {
        var outcome = Resolve(Side(Kingdom(gold: 50, fortification: -1), Order(buy: [PurchaseType.Repair])));

        outcome.Player1.Kingdom.Fortification.ShouldBe(0);
        outcome.Player1.Kingdom.Gold.ShouldBe(38);
    }

    [Theory]
    [InlineData(PurchaseType.RepairToOne, 1, 16)]
    [InlineData(PurchaseType.RepairToTwo, 2, 19)]
    public void Lost_levels_are_bought_back_cheap_the_round_after_the_fall(PurchaseType offer, int level, int cost)
    {
        var outcome = Resolve(Side(
            Kingdom(gold: 50, fortification: -1, ruinedFrom: 2),
            Order(buy: [PurchaseType.Repair, offer])));

        outcome.Player1.Kingdom.Fortification.ShouldBe(level);
        outcome.Player1.Kingdom.Gold.ShouldBe(50 - cost);
    }

    [Fact]
    public void Only_the_levels_the_wall_had_can_be_bought_back()
    {
        var outcome = Resolve(Side(
            Kingdom(gold: 50, fortification: -1, ruinedFrom: 1),
            Order(buy: [PurchaseType.Repair, PurchaseType.RepairToTwo])));

        outcome.Player1.Kingdom.Fortification.ShouldBe(0);
        outcome.Player1.Kingdom.Gold.ShouldBe(38);
    }

    [Fact]
    public void The_cheap_offer_lasts_a_single_round()
    {
        Resolve(Side(Kingdom(fortification: -1, ruinedFrom: 2))).Player1.Kingdom.RuinedFrom.ShouldBe(0);
    }

    [Fact]
    public void Several_purchases_go_through_together()
    {
        var outcome = Resolve(Side(
            Kingdom(gold: 50),
            Order(feast: FeastTier.Small, buy: [PurchaseType.Fortify, PurchaseType.Catapult, PurchaseType.Recruit])));

        outcome.Player1.Kingdom.Gold.ShouldBe(50 - 5 - 12 - 15 - 12);
        outcome.Player1.Kingdom.Fortification.ShouldBe(1);
        outcome.Player1.Kingdom.Catapults.ShouldBe(1);
        outcome.Player1.Kingdom.Troops.ShouldBe(16);
    }
}
