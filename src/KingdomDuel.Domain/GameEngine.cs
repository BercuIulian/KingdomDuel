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
    private readonly Random _random;

    public GameEngine(Random? random = null)
    {
        _random = random ?? Random.Shared;
    }

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
        ActionType player1Action,
        ActionType player2Action)
    {
        var log = new List<string>();

        ApplyRebellion(player1, currentEvent, log, "Jucătorul 1");
        ApplyRebellion(player2, currentEvent, log, "Jucătorul 2");

        ApplySelfAction(player1, player2, player1Action, currentEvent, log, "Jucătorul 1");
        ApplySelfAction(player2, player1, player2Action, currentEvent, log, "Jucătorul 2");

        ResolveInteraction(player1, player2, player1Action, player2Action, log);

        var gameOverReason = EvaluateGameOver(player1, player2, roundNumber, totalRounds);

        return new RoundResult
        {
            RoundNumber = roundNumber,
            Event = currentEvent,
            Player1Action = player1Action,
            Player2Action = player2Action,
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
        Kingdom opponent,
        ActionType action,
        GameEventType currentEvent,
        List<string> log,
        string label)
    {
        switch (action)
        {
            case ActionType.Attack:
            {
                side.DefenseStreak = 0;
                var cost = currentEvent == GameEventType.PeaceRumors
                    ? GameConstants.AttackTroopCostDuringPeaceRumors
                    : GameConstants.AttackTroopCost;
                side.Troops = Clamp0(side.Troops - cost);
                log.Add($"{label} — atac lansat: -{cost} trupe");
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
                if (side.TotalPower >= opponent.TotalPower)
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
        }
    }

    private void ResolveInteraction(
        Kingdom player1,
        Kingdom player2,
        ActionType player1Action,
        ActionType player2Action,
        List<string> log)
    {
        var player1Attacks = player1Action == ActionType.Attack;
        var player2Attacks = player2Action == ActionType.Attack;

        if (player1Attacks && player2Attacks)
        {
            ResolveMutualClash(player1, player2, log);
        }
        else if (player1Attacks)
        {
            ResolveOneSidedAttack(player1, player2, player2Action, log, "Jucătorul 1", "Jucătorul 2");
        }
        else if (player2Attacks)
        {
            ResolveOneSidedAttack(player2, player1, player1Action, log, "Jucătorul 2", "Jucătorul 1");
        }
        else if (player1Action == ActionType.Diplomacy && player2Action == ActionType.Diplomacy)
        {
            player1.Gold += GameConstants.DiplomacyAllianceGoldBonus;
            player2.Gold += GameConstants.DiplomacyAllianceGoldBonus;
            log.Add($"Alianță diplomatică reciprocă: ambele tabere +{GameConstants.DiplomacyAllianceGoldBonus} aur.");
        }
    }

    private void ResolveMutualClash(Kingdom player1, Kingdom player2, List<string> log)
    {
        var moraleDiff = player1.Morale - player2.Morale;
        var factor = Math.Clamp(
            (int)Math.Round(moraleDiff / (double)GameConstants.MutualClashMoralePerShiftPoint),
            -GameConstants.MutualClashMaxMoraleShiftFactor,
            GameConstants.MutualClashMaxMoraleShiftFactor);

        var player1Loss = Clamp0(GameConstants.MutualClashBaseTroopLoss - factor);
        var player2Loss = Clamp0(GameConstants.MutualClashBaseTroopLoss + factor);

        player1.Troops = Clamp0(player1.Troops - player1Loss);
        player2.Troops = Clamp0(player2.Troops - player2Loss);
        player1.Morale = Clamp0(player1.Morale - GameConstants.MutualClashMoraleLoss);
        player2.Morale = Clamp0(player2.Morale - GameConstants.MutualClashMoraleLoss);

        if (factor > 0)
        {
            log.Add($"Ciocnire directă: moralul mai mare al Jucătorului 1 îi întărește trupele — pierde doar {player1Loss} trupe, Jucătorul 2 pierde {player2Loss}. Ambele tabere -{GameConstants.MutualClashMoraleLoss} moral.");
        }
        else if (factor < 0)
        {
            log.Add($"Ciocnire directă: moralul mai mare al Jucătorului 2 îi întărește trupele — pierde doar {player2Loss} trupe, Jucătorul 1 pierde {player1Loss}. Ambele tabere -{GameConstants.MutualClashMoraleLoss} moral.");
        }
        else
        {
            log.Add($"Ciocnire directă, moral egal: ambele tabere pierd {player1Loss} trupe și {GameConstants.MutualClashMoraleLoss} moral.");
        }
    }

    private void ResolveOneSidedAttack(
        Kingdom attacker,
        Kingdom defender,
        ActionType defenderAction,
        List<string> log,
        string attackerLabel,
        string defenderLabel)
    {
        if (defenderAction == ActionType.Defend)
        {
            var attackerLevel = attacker.EquipmentLevel;
            var defenderLevel = defender.EquipmentLevel;
            var chance = Math.Clamp(
                GameConstants.BreakthroughBaseChance + (attackerLevel - defenderLevel) * GameConstants.BreakthroughChancePerLevel,
                GameConstants.BreakthroughMinChance,
                GameConstants.BreakthroughMaxChance);

            if (_random.NextDouble() < chance)
            {
                var takenGold = Math.Min(defender.Gold, GameConstants.BreakthroughStealGold);
                var takenTroops = Math.Min(defender.Troops, GameConstants.BreakthroughStealTroops);
                defender.Gold = Clamp0(defender.Gold - takenGold);
                defender.Troops = Clamp0(defender.Troops - takenTroops);
                attacker.Gold += takenGold;
                attacker.Troops += takenTroops;
                attacker.Morale += GameConstants.UndefendedAttackerMoraleGain;

                log.Add($"{attackerLabel} străpunge apărarea lui {defenderLabel} datorită echipamentului mai bun (nivel {attackerLevel} vs {defenderLevel}) — șansă {Math.Round(chance * 100)}%. Fură {takenGold} aur și {takenTroops} trupe.");
            }
            else
            {
                var extraLoss = Math.Max(GameConstants.RepelledExtraTroopLossMin, GameConstants.RepelledExtraTroopLossBase - attackerLevel);
                attacker.Troops = Clamp0(attacker.Troops - extraLoss);
                var moraleBonus = GameConstants.RepelledDefenderMoraleBase + defenderLevel;
                defender.Morale += moraleBonus;

                log.Add($"{defenderLabel} respinge atacul lui {attackerLabel} (fortificație nivel {defenderLevel}, șansă de străpungere {Math.Round(chance * 100)}%): {attackerLabel} mai pierde {extraLoss} trupe, {defenderLabel} +{moraleBonus} moral.");
            }
        }
        else
        {
            var isDiplomacy = defenderAction == ActionType.Diplomacy;
            var stealGold = isDiplomacy ? GameConstants.UndefendedDiplomacyStealGold : GameConstants.UndefendedStealGold;
            var stealTroops = isDiplomacy ? GameConstants.UndefendedDiplomacyStealTroops : GameConstants.UndefendedStealTroops;

            var takenGold = Math.Min(defender.Gold, stealGold);
            var takenTroops = Math.Min(defender.Troops, stealTroops);
            defender.Gold = Clamp0(defender.Gold - takenGold);
            defender.Troops = Clamp0(defender.Troops - takenTroops);
            attacker.Gold += takenGold;
            attacker.Troops += takenTroops;
            attacker.Morale += GameConstants.UndefendedAttackerMoraleGain;

            log.Add($"{attackerLabel} atacă cu succes: fură {takenGold} aur și {takenTroops} trupe de la {defenderLabel}.");
        }
    }

    private static int Clamp0(int value) => Math.Max(0, value);
}
