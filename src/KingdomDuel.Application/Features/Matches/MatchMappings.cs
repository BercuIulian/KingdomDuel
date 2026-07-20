using KingdomDuel.Domain.Entities.Matches;
using KingdomDuel.Shared.Dtos.Matches;

namespace KingdomDuel.Application.Features.Matches;

internal static class MatchMappings
{
    public static MatchStateDto ToStateDto(this Match match) => new()
    {
        Code = match.Code,
        Status = match.Status,
        Round = match.Round,
        CurrentEvent = match.CurrentEvent,
        GameOverReason = match.GameOverReason,
        Player1Name = match.Player1?.DisplayName,
        Player2Name = match.Player2?.DisplayName,
        Player1Kingdom = match.Player1?.Kingdom.ToKingdomDto(),
        Player2Kingdom = match.Player2?.Kingdom.ToKingdomDto(),
        WaitingOnPlayer1Action =
            match.Player1 is not null && !match.PendingActions.ContainsKey(match.Player1.PlayerId),
        WaitingOnPlayer2Action =
            match.Player2 is not null && !match.PendingActions.ContainsKey(match.Player2.PlayerId),
        History = match.History.OrderBy(r => r.RoundNumber).Select(r => r.ToRoundResultDto()).ToList(),
    };

    private static KingdomDto ToKingdomDto(this Kingdom kingdom) => new()
    {
        Gold = kingdom.Gold,
        Troops = kingdom.Troops,
        Morale = kingdom.Morale,
        EquipmentLevel = kingdom.EquipmentLevel,
        TotalPower = kingdom.TotalPower,
    };

    private static RoundResultDto ToRoundResultDto(this RoundResult result) => new()
    {
        RoundNumber = result.RoundNumber,
        Event = result.Event,
        Player1Action = result.Player1Action,
        Player2Action = result.Player2Action,
        Log = result.Log.ToList(),
    };
}
