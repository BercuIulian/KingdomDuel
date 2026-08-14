using KingdomDuel.Domain.Entities.Matches;
using KingdomDuel.Shared.Dtos.Matches;

namespace KingdomDuel.Application.Features.Matches;

internal static class MatchMappings
{
    public static MatchStateDto ToStateDto(this Match match, Guid userId) => new()
    {
        Code = match.Code,
        YourSide = match.SideOf(userId),
        IsHost = match.Player1?.UserId == userId,
        Status = match.Status,
        Round = match.Round,
        CurrentEvent = match.CurrentEvent,
        GameOverReason = match.GameOverReason,
        TotalRounds = match.TotalRounds,
        StartingGold = match.StartingGold,
        StartingTroops = match.StartingTroops,
        StartingMorale = match.StartingMorale,
        RoundSeconds = match.RoundSeconds,
        Player1Name = match.Player1?.User.UserName,
        Player2Name = match.Player2?.User.UserName,
        Player1Ready = match.Player1?.IsReady ?? false,
        Player2Ready = match.Player2?.IsReady ?? false,
        Player1Kingdom = match.Player1?.Kingdom.ToKingdomDto(),
        Player2Kingdom = match.Player2?.Kingdom.ToKingdomDto(),
        WaitingOnPlayer1Action =
            match.Player1 is not null && !match.PendingActions.ContainsKey(match.Player1.ActionKey()),
        WaitingOnPlayer2Action =
            match.Player2 is not null && !match.PendingActions.ContainsKey(match.Player2.ActionKey()),
        History = match.History.OrderBy(r => r.RoundNumber).Select(r => r.ToRoundResultDto()).ToList(),
    };

    public static int SideOf(this Match match, Guid userId)
    {
        if (match.Player1?.UserId == userId)
        {
            return 1;
        }

        return match.Player2?.UserId == userId ? 2 : 0;
    }

    public static PlayerSlot? SlotOf(this Match match, Guid userId)
        => match.SideOf(userId) switch
        {
            1 => match.Player1,
            2 => match.Player2,
            _ => null,
        };

    public static string ActionKey(this PlayerSlot slot) => slot.UserId.ToString();

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
