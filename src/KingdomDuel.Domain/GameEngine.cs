using KingdomDuel.Domain.Entities.Matches;
using KingdomDuel.Domain.Enums;

namespace KingdomDuel.Domain;

/// <summary>
/// Stateless resolver for a single round. Mutates the two Kingdom instances
/// it is given and returns a RoundResult describing what happened. Every
/// number used here comes from GameConstants; every rule is documented in
/// RULES.md - if you change one, change both.
/// </summary>
public class GameEngine
{
    private const string Player1Label = "Jucătorul 1";
    private const string Player2Label = "Jucătorul 2";

    private readonly Random _random;

    public GameEngine(Random? random = null)
    {
        _random = random ?? Random.Shared;
    }

    /// <summary>
    /// Câte trupe se pot trimite cel mult într-o expediție în runda asta. Zvonurile
    /// de pace coboară plafonul: armatele ezită, deci nu poți strânge o oaste mare.
    /// </summary>
    public static int MaxAttackTroopsFor(GameEventType currentEvent)
        => currentEvent == GameEventType.PeaceRumors
            ? GameConstants.MaxAttackTroopsDuringPeaceRumors
            : GameConstants.MaxAttackTroops;

    /// <summary>
    /// Șansa ca o expediție de mărimea dată să străpungă o fortificație. Publică
    /// fiindcă interfața arată aceste procente înainte de a trimite ordinul — și
    /// trebuie să fie exact cifra pe care o va folosi motorul, nu o copie a ei.
    /// </summary>
    public static double BreakthroughChanceFor(int committedTroops, int attackerEquipmentLevel, int defenderEquipmentLevel)
        => Math.Clamp(
            GameConstants.BreakthroughMinChance
            + GameConstants.BreakthroughCommitmentSpan * Strength(committedTroops)
            + GameConstants.BreakthroughChancePerLevel * (attackerEquipmentLevel - defenderEquipmentLevel),
            GameConstants.BreakthroughMinChance,
            GameConstants.BreakthroughMaxChance);

    /// <summary>
    /// Șansa ca expediția să treacă peste un regat care nu s-a fortificat. O oaste
    /// completă trece sigur; una mică poate fi alungată.
    /// </summary>
    public static double RaidChanceFor(int committedTroops)
        => GameConstants.RaidMinChance + (1 - GameConstants.RaidMinChance) * Strength(committedTroops);

    public GameEventType PickRandomEvent()
    {
        var values = Enum.GetValues<GameEventType>();
        return values[_random.Next(values.Length)];
    }

    public void InitializeKingdoms(
        Kingdom player1,
        Kingdom player2,
        int startingGold,
        int startingTroops,
        int startingMorale)
    {
        foreach (var side in new[] { player1, player2 })
        {
            side.Gold = startingGold;
            side.Troops = startingTroops;
            side.Morale = startingMorale;
            side.DefenseStreak = 0;
        }
    }

    public RoundResult ResolveRound(
        int roundNumber,
        int totalRounds,
        GameEventType currentEvent,
        Kingdom player1,
        Kingdom player2,
        PendingOrder player1Order,
        PendingOrder player2Order)
    {
        var log = new List<string>();

        ApplyRebellion(player1, currentEvent, log, Player1Label);
        ApplyRebellion(player2, currentEvent, log, Player2Label);

        // Expediția se plafonează după rebeliune: dacă răsculații ți-au luat trupele,
        // nu mai ai ce trimite.
        var player1Troops = CommittedTroops(player1, player1Order, currentEvent);
        var player2Troops = CommittedTroops(player2, player2Order, currentEvent);

        // Regula de stagnare se uită la puterea de la începutul rundei, nu la una
        // mutată deja de ordinul celuilalt — altfel verdictul ar depinde de ordinea
        // în care aplicăm cele două ordine.
        var player1Ahead = player1.TotalPower >= player2.TotalPower;
        var player2Ahead = player2.TotalPower >= player1.TotalPower;

        ApplySelfAction(player1, player1Order.Action, player1Troops, currentEvent, log, Player1Label, player1Ahead);
        ApplySelfAction(player2, player2Order.Action, player2Troops, currentEvent, log, Player2Label, player2Ahead);

        ResolveInteraction(
            player1, player2,
            player1Order.Action, player2Order.Action,
            player1Troops, player2Troops,
            log);

        var gameOverReason = EvaluateGameOver(player1, player2, roundNumber, totalRounds);

        return new RoundResult
        {
            RoundNumber = roundNumber,
            Event = currentEvent,
            Player1Action = player1Order.Action,
            Player2Action = player2Order.Action,
            Player1AttackTroops = player1Troops,
            Player2AttackTroops = player2Troops,
            Log = log,
            GameOverReason = gameOverReason
        };
    }

    public GameOverReason EvaluateGameOver(Kingdom player1, Kingdom player2, int roundNumber, int totalRounds)
    {
        var player1Down = player1.Troops <= 0 || player1.Morale <= 0;
        var player2Down = player2.Troops <= 0 || player2.Morale <= 0;

        if (player1Down && player2Down) return GameOverReason.BothCollapsed;
        if (player1Down) return GameOverReason.Player1Collapsed;
        if (player2Down) return GameOverReason.Player2Collapsed;

        if (roundNumber >= totalRounds)
        {
            if (player1.TotalPower > player2.TotalPower) return GameOverReason.RoundsExhaustedPlayer1Wins;
            if (player2.TotalPower > player1.TotalPower) return GameOverReason.RoundsExhaustedPlayer2Wins;
            return GameOverReason.RoundsExhaustedDraw;
        }

        return GameOverReason.None;
    }

    /// <summary>
    /// Cât pleacă de fapt: declarația jucătorului, tăiată la plafonul rundei și la
    /// oastea pe care o are. Zero înseamnă că atacul nu are loc.
    /// </summary>
    private static int CommittedTroops(Kingdom side, PendingOrder order, GameEventType currentEvent)
    {
        if (order.Action != ActionType.Attack)
        {
            return 0;
        }

        var ceiling = Math.Min(MaxAttackTroopsFor(currentEvent), side.Troops);

        return ceiling < GameConstants.MinAttackTroops
            ? 0
            : Math.Clamp(order.Troops, GameConstants.MinAttackTroops, ceiling);
    }

    private static void ApplyRebellion(Kingdom side, GameEventType currentEvent, List<string> log, string label)
    {
        if (currentEvent == GameEventType.Rebellion && side.Morale < GameConstants.RebellionMoraleThreshold)
        {
            side.Troops = Clamp0(side.Troops - GameConstants.RebellionTroopLoss);
            log.Add($"{label} — rebeliune internă: -{GameConstants.RebellionTroopLoss} trupe");
        }
    }

    private static void ApplySelfAction(
        Kingdom side,
        ActionType action,
        int committedTroops,
        GameEventType currentEvent,
        List<string> log,
        string label,
        bool alreadyAhead)
    {
        switch (action)
        {
            case ActionType.Attack:
            {
                side.DefenseStreak = 0;

                // Trupele nu se scad aici: pleacă în expediție și se întorc cele care
                // scapă. Pierderile se aplică la rezolvarea întâlnirii.
                log.Add(committedTroops > 0
                    ? $"{label} — trimite {committedTroops} trupe în expediție"
                    : $"{label} — a vrut să atace, dar n-a avut nicio trupă de trimis");
                break;
            }

            case ActionType.Defend:
            {
                side.DefenseStreak += 1;
                var streak = side.DefenseStreak;
                var goldCost = GameConstants.DefenseBaseGoldCost + (streak - 1) * GameConstants.DefenseGoldCostStep;
                side.Gold = Clamp0(side.Gold - goldCost);

                var moraleGain = Math.Max(
                    GameConstants.DefenseMinMoraleGain,
                    GameConstants.DefenseBaseMoraleGain - (streak - 1) * GameConstants.DefenseMoraleGainStep);

                var stagnationNote = string.Empty;
                if (alreadyAhead)
                {
                    moraleGain -= GameConstants.StagnationMoralePenalty;
                    stagnationNote = $" (stagnare: -{GameConstants.StagnationMoralePenalty} fiindcă e deja în avantaj)";
                }

                side.Morale = Clamp0(side.Morale + moraleGain);
                var sign = moraleGain >= 0 ? "+" : string.Empty;
                log.Add($"{label} — fortificare ({streak}x la rând): -{goldCost} aur, {sign}{moraleGain} moral{stagnationNote}");
                break;
            }

            case ActionType.Trade:
            {
                side.DefenseStreak = 0;
                var goldGain = currentEvent switch
                {
                    GameEventType.Drought => GameConstants.TradeDroughtGoldGain,
                    GameEventType.Harvest => GameConstants.TradeHarvestGoldGain,
                    _ => GameConstants.TradeBaseGoldGain
                };
                side.Gold += goldGain;
                side.Morale = Clamp0(side.Morale + GameConstants.TradeMoraleGain);
                log.Add($"{label} — comerț: +{goldGain} aur, +{GameConstants.TradeMoraleGain} moral");
                break;
            }

            case ActionType.Diplomacy:
            {
                side.DefenseStreak = 0;
                side.Gold = Clamp0(side.Gold - GameConstants.DiplomacyGoldCost);
                var moraleGain = currentEvent == GameEventType.DiplomaticFair
                    ? GameConstants.DiplomacyFairMoraleGain
                    : GameConstants.DiplomacyBaseMoraleGain;
                side.Morale += moraleGain;
                log.Add($"{label} — diplomație: -{GameConstants.DiplomacyGoldCost} aur, +{moraleGain} moral");
                break;
            }

            case ActionType.Idle:
            {
                side.DefenseStreak = 0;
                log.Add($"{label} — fără ordin: nimic nu se schimbă");
                break;
            }
        }
    }

    private void ResolveInteraction(
        Kingdom player1,
        Kingdom player2,
        ActionType player1Action,
        ActionType player2Action,
        int player1Troops,
        int player2Troops,
        List<string> log)
    {
        var player1Attacks = player1Action == ActionType.Attack && player1Troops > 0;
        var player2Attacks = player2Action == ActionType.Attack && player2Troops > 0;

        if (player1Attacks && player2Attacks)
        {
            ResolveClash(player1, player2, player1Troops, player2Troops, log);
        }
        else if (player1Attacks)
        {
            ResolveExpedition(player1, player2, player2Action, player1Troops, log, Player1Label, Player2Label);
        }
        else if (player2Attacks)
        {
            ResolveExpedition(player2, player1, player1Action, player2Troops, log, Player2Label, Player1Label);
        }
        else if (player1Action == ActionType.Diplomacy && player2Action == ActionType.Diplomacy)
        {
            player1.Gold += GameConstants.DiplomacyAllianceGoldBonus;
            player2.Gold += GameConstants.DiplomacyAllianceGoldBonus;
            log.Add($"Alianță diplomatică reciprocă: ambele tabere +{GameConstants.DiplomacyAllianceGoldBonus} aur.");
        }
    }

    /// <summary>
    /// Două expediții se întâlnesc în câmp deschis. Cine a trimis mai mult pierde o
    /// parte mai mică din ce a trimis, iar moralul mai mare valorează trupe în plus
    /// la socoteala asta, fără să schimbe câte au plecat de fapt.
    /// </summary>
    private void ResolveClash(Kingdom player1, Kingdom player2, int player1Troops, int player2Troops, List<string> log)
    {
        var moraleEdge = Math.Clamp(
            (int)Math.Round((player1.Morale - player2.Morale) / (double)GameConstants.MutualClashMoralePerShiftPoint),
            -GameConstants.MutualClashMaxMoraleShiftFactor,
            GameConstants.MutualClashMaxMoraleShiftFactor);

        var player1Field = Math.Max(1, player1Troops + moraleEdge);
        var player2Field = Math.Max(1, player2Troops - moraleEdge);
        var player1Share = player1Field / (double)(player1Field + player2Field);

        var player1Losses = Casualties(player1Troops, LossFractionInClash(1 - player1Share));
        var player2Losses = Casualties(player2Troops, LossFractionInClash(player1Share));

        player1.Troops = Clamp0(player1.Troops - player1Losses);
        player2.Troops = Clamp0(player2.Troops - player2Losses);
        player1.Morale = Clamp0(player1.Morale - GameConstants.MutualClashMoraleLoss);
        player2.Morale = Clamp0(player2.Morale - GameConstants.MutualClashMoraleLoss);

        var edgeNote = moraleEdge switch
        {
            > 0 => $" Moralul mai bun al Jucătorului 1 a valorat {moraleEdge} trupe în plus pe câmp.",
            < 0 => $" Moralul mai bun al Jucătorului 2 a valorat {-moraleEdge} trupe în plus pe câmp.",
            _ => string.Empty,
        };

        log.Add(
            $"Ciocnire în câmp deschis: {player1Troops} trupe contra {player2Troops}. "
            + $"Jucătorul 1 pierde {player1Losses} trupe, Jucătorul 2 pierde {player2Losses}. "
            + $"Ambele tabere -{GameConstants.MutualClashMoraleLoss} moral.{edgeNote}");
    }

    private void ResolveExpedition(
        Kingdom attacker,
        Kingdom defender,
        ActionType defenderAction,
        int committedTroops,
        List<string> log,
        string attackerLabel,
        string defenderLabel)
    {
        var strength = Strength(committedTroops);

        if (defenderAction == ActionType.Defend)
        {
            var chance = BreakthroughChanceFor(committedTroops, attacker.EquipmentLevel, defender.EquipmentLevel);

            if (_random.NextDouble() < chance)
            {
                var losses = Plunder(
                    attacker, defender, strength, committedTroops,
                    GameConstants.BreakthroughStealGold,
                    GameConstants.BreakthroughStealTroops,
                    GameConstants.BreakthroughLossFraction,
                    out var takenGold, out var takenTroops, out var moraleGain);

                log.Add(
                    $"{attackerLabel} străpunge fortificația lui {defenderLabel} cu {committedTroops} trupe "
                    + $"(echipament nivel {attacker.EquipmentLevel} contra {defender.EquipmentLevel}, șansă {Percent(chance)}%): "
                    + $"fură {takenGold} aur și {takenTroops} trupe, +{moraleGain} moral, dar {losses} trupe nu se mai întorc.");
            }
            else
            {
                var losses = Casualties(committedTroops, GameConstants.RepelledLossFraction);
                attacker.Troops = Clamp0(attacker.Troops - losses);
                var moraleBonus = GameConstants.RepelledDefenderMoraleBase + defender.EquipmentLevel;
                defender.Morale += moraleBonus;

                log.Add(
                    $"{defenderLabel} frânge asaltul lui {attackerLabel} pe ziduri "
                    + $"(fortificație nivel {defender.EquipmentLevel}, șansă de străpungere {Percent(chance)}%): "
                    + $"cele {losses} trupe trimise se pierd, {defenderLabel} +{moraleBonus} moral.");
            }

            return;
        }

        // Fără fortificație, regatul se apară cu ce are la îndemână: o expediție mică
        // poate fi alungată, una completă trece sigur.
        var raidChance = RaidChanceFor(committedTroops);

        if (_random.NextDouble() >= raidChance)
        {
            var losses = Casualties(committedTroops, GameConstants.RaidChasedOffLossFraction);
            attacker.Troops = Clamp0(attacker.Troops - losses);
            defender.Morale += GameConstants.ChasedOffDefenderMoraleGain;

            log.Add(
                $"Cele {committedTroops} trupe ale lui {attackerLabel} sunt prea puține și {defenderLabel} le alungă "
                + $"fără să ridice ziduri (șansă de reușită {Percent(raidChance)}%): "
                + $"{losses} trupe pierdute, {defenderLabel} +{GameConstants.ChasedOffDefenderMoraleGain} moral.");
            return;
        }

        var isDiplomacy = defenderAction == ActionType.Diplomacy;

        var spoils = Plunder(
            attacker, defender, strength, committedTroops,
            isDiplomacy ? GameConstants.UndefendedDiplomacyStealGold : GameConstants.UndefendedStealGold,
            isDiplomacy ? GameConstants.UndefendedDiplomacyStealTroops : GameConstants.UndefendedStealTroops,
            GameConstants.RaidSuccessLossFraction,
            out var raidedGold, out var raidedTroops, out var raidMorale);

        log.Add(
            $"{attackerLabel} lovește nestingherit cu {committedTroops} trupe: "
            + $"fură {raidedGold} aur și {raidedTroops} trupe de la {defenderLabel}, +{raidMorale} moral, "
            + $"{spoils} trupe pierdute pe drum.");
    }

    /// <summary>
    /// Mută prada la atacator, îi scade pierderile de expediție și întoarce câte
    /// trupe nu s-au mai întors. Prada e proporțională cu cât s-a trimis.
    /// </summary>
    private static int Plunder(
        Kingdom attacker,
        Kingdom defender,
        double strength,
        int committedTroops,
        int goldAtFullCommitment,
        int troopsAtFullCommitment,
        double lossFraction,
        out int takenGold,
        out int takenTroops,
        out int moraleGain)
    {
        takenGold = Math.Min(defender.Gold, Scaled(goldAtFullCommitment, strength));
        takenTroops = Math.Min(defender.Troops, Scaled(troopsAtFullCommitment, strength));
        moraleGain = Scaled(GameConstants.UndefendedAttackerMoraleGain, strength);

        defender.Gold = Clamp0(defender.Gold - takenGold);
        defender.Troops = Clamp0(defender.Troops - takenTroops);

        var losses = Casualties(committedTroops, lossFraction);

        attacker.Gold += takenGold;
        attacker.Troops = Clamp0(attacker.Troops + takenTroops - losses);
        attacker.Morale += moraleGain;

        return losses;
    }

    private static double Strength(int committedTroops)
        => committedTroops / (double)GameConstants.MaxAttackTroops;

    private static double LossFractionInClash(double weakness)
        => GameConstants.ClashBaseLossFraction + GameConstants.ClashLossSwing * weakness;

    /// <summary>
    /// Pierderile se rotunjesc în sus: cine își trimite toată oastea într-o
    /// înfrângere rămâne fără ea, și regatul cade.
    /// </summary>
    private static int Casualties(int committedTroops, double lossFraction)
        => Math.Min(committedTroops, (int)Math.Ceiling(committedTroops * lossFraction));

    private static int Scaled(int atFullCommitment, double strength)
        => Math.Max(1, (int)Math.Round(atFullCommitment * strength));

    private static int Percent(double chance) => (int)Math.Round(chance * 100);

    private static int Clamp0(int value) => Math.Max(0, value);
}
