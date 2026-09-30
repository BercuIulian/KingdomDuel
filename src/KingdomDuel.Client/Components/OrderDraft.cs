using KingdomDuel.Domain;
using KingdomDuel.Domain.Enums;
using KingdomDuel.Domain.Rounds;
using KingdomDuel.Shared.Dtos.Matches;
using KingdomDuel.Shared.Requests.Matches;

namespace KingdomDuel.Client.Components;

public sealed class OrderDraft(MatchStateDto state, int repairTo = -1)
{
    private readonly Dictionary<ActionType, int> allocation = new();
    private readonly HashSet<PurchaseType> bought = [];

    public MatchStateDto State { get; set; } = state;

    public int RepairTo { get; set; } = repairTo;

    public FeastTier? Feast { get; set; }

    public int Hired { get; set; }

    public int Armed { get; set; }

    public SpyTier? Spies { get; private set; }

    public bool RecallSpies { get; private set; }

    public HuntTier? Hunt { get; set; }

    public IReadOnlyCollection<PurchaseType> Bought => bought;

    public KingdomDto? Mine => State.YourSide == 1 ? State.Player1Kingdom : State.Player2Kingdom;

    public CaravanDto? Caravan => State.YourCaravan;

    public SpyRingDto? SpiesOut => State.YourSpies;

    public int Troops => Mine?.Troops ?? 0;

    public int Gold => Mine?.Gold ?? 0;

    public int Villagers => Mine?.Villagers ?? 0;

    public bool WallDown => (Mine?.Fortification ?? 0) <= GameConstants.WallDownLevel;

    public int Fighting => Allocated(ActionType.Attack) + Allocated(ActionType.Defend);

    public int Free => Math.Max(0, Troops - Fighting - (Hunt is { } chase ? GameConstants.HuntSoldiers(chase) : 0));

    public int FreeVillagers => Math.Max(0, Villagers - Allocated(ActionType.Trade) - Armed - LeavingToSpy);

    public SpyTier? ChosenSpies => RecallSpies ? null : Spies ?? SpiesOut?.Tier;

    public int GoldSpent =>
        (Feast is { } tier ? GameConstants.FeastCost(tier) : 0)
        + bought.Sum(GameConstants.PurchaseCost)
        + Repairs.Sum(GameConstants.PurchaseCost)
        + Hired * GameConstants.VillagerBundleCost
        + Armed * GameConstants.ArmCost
        + (ChosenSpies is { } spied ? GameConstants.SpyUpkeep(spied) : 0)
        + (Hunt is { } chased ? GameConstants.HuntCost(chased) : 0);

    public int GoldLeft => Gold - GoldSpent;

    public bool Fits =>
        Fighting <= Troops
        && Allocated(ActionType.Trade) + Armed + LeavingToSpy <= Villagers
        && GoldSpent <= Gold;

    public IEnumerable<PurchaseType> Repairs
    {
        get
        {
            if (RepairTo < 0)
            {
                yield break;
            }

            yield return PurchaseType.Repair;

            if (RepairTo == 1) yield return PurchaseType.RepairToOne;
            if (RepairTo == 2) yield return PurchaseType.RepairToTwo;
        }
    }

    public int MaxBundles => Math.Max(Hired, Purse / GameConstants.VillagerBundleCost);

    public int MaxArmed => Math.Max(Armed, Math.Min(Villagers - Allocated(ActionType.Trade), Purse / Math.Max(1, GameConstants.ArmCost)));

    private int Purse => Gold - GoldSpent + Hired * GameConstants.VillagerBundleCost + Armed * GameConstants.ArmCost;

    private int LeavingToSpy => Math.Max(0, SpyStaff - (SpiesOut?.Villagers ?? 0));

    private int SpyStaff => ChosenSpies is { } tier ? GameConstants.SpyVillagers(tier) : 0;

    public int Allocated(ActionType direction) => allocation.GetValueOrDefault(direction);

    public void Allocate(ActionType direction, int troops) => allocation[direction] = troops;

    public void Withdraw(ActionType direction) => allocation.Remove(direction);

    public bool Blocked(ActionType direction) =>
        (direction == ActionType.Trade && Caravan is not null)
        || (direction == ActionType.Defend && WallDown);

    public int Ceiling(ActionType direction) => direction == ActionType.Trade
        ? FreeVillagers + Allocated(direction)
        : Free + Allocated(direction);

    public int FreeForHunt(HuntTier tier) =>
        Free + (Hunt == tier ? GameConstants.HuntSoldiers(tier) : 0);

    public static int MaxLevel(PurchaseType purchase) => purchase switch
    {
        PurchaseType.Fortify => GameConstants.FortifyMaxLevel,
        PurchaseType.Catapult => GameConstants.CatapultMaxLevel,
        _ => 0,
    };

    public int Level(PurchaseType purchase) => purchase switch
    {
        PurchaseType.Fortify => Mine?.Fortification ?? 0,
        PurchaseType.Catapult => Mine?.Catapults ?? 0,
        _ => 0,
    };

    public bool AtMax(PurchaseType purchase) => MaxLevel(purchase) > 0 && Level(purchase) >= MaxLevel(purchase);

    public int MoraleFrom(FeastTier tier)
    {
        var sent = Allocated(ActionType.Trade);
        var spiesBack = Math.Max(0, (SpiesOut?.Villagers ?? 0) - SpyStaff);

        return GameEngine.FeastMorale(
            tier,
            GameEngine.FeastWeight(Troops - Fighting, Villagers - sent - LeavingToSpy + spiesBack),
            GameEngine.FeastWeight(Fighting, (Caravan?.Troops ?? 0) + sent + SpyStaff),
            Math.Max(0, (Mine?.Morale ?? 0) - GameConstants.MoraleDecayPerRound));
    }

    public bool CanSendSpies(SpyTier tier) =>
        GameConstants.SpyVillagers(tier) - (SpiesOut?.Villagers ?? 0)
        <= Villagers - Allocated(ActionType.Trade) - Armed;

    public bool Affordable(FeastTier tier) =>
        GameConstants.FeastCost(tier) + bought.Sum(GameConstants.PurchaseCost) <= Gold;

    public bool Buyable(PurchaseType purchase)
    {
        if (purchase is PurchaseType.RepairToOne or PurchaseType.RepairToTwo
            && !bought.Contains(PurchaseType.Repair))
        {
            return false;
        }

        return !AtMax(purchase)
            && (bought.Contains(purchase) || GameConstants.PurchaseCost(purchase) <= GoldLeft);
    }

    public void Toggle(PurchaseType purchase)
    {
        if (!bought.Remove(purchase))
        {
            bought.Add(purchase);

            if (purchase == PurchaseType.RepairToOne) bought.Remove(PurchaseType.RepairToTwo);
            if (purchase == PurchaseType.RepairToTwo) bought.Remove(PurchaseType.RepairToOne);
        }
        else if (purchase == PurchaseType.Repair)
        {
            bought.Remove(PurchaseType.RepairToOne);
            bought.Remove(PurchaseType.RepairToTwo);
        }
    }

    public void PickSpies(SpyTier? tier)
    {
        Spies = tier;
        RecallSpies = false;
    }

    public void CallSpiesHome()
    {
        Spies = null;
        RecallSpies = true;
    }

    public void PickHunt(HuntTier tier) => Hunt = Hunt == tier ? null : tier;

    public SubmitOrderRequest ToRequest() => new()
    {
        Code = State.Code,
        Troops = allocation
            .Where(pair => pair.Value > 0)
            .Select(pair => new OrderAllocationDto { Direction = pair.Key, Troops = pair.Value })
            .ToList(),
        Feast = Feast,
        Purchases = [.. bought, .. Repairs],
        Hired = Hired,
        Armed = Armed,
        Spies = Spies,
        RecallSpies = RecallSpies,
        Hunt = Hunt,
    };
}
