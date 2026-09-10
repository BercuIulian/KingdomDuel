using KingdomDuel.Domain.Entities.Matches;
using KingdomDuel.Domain.Enums;
using KingdomDuel.Domain.Rounds;
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
        SecondsRemaining = match.SecondsRemaining(),
        Player1Name = match.Player1?.User.UserName,
        Player2Name = match.Player2?.User.UserName,
        Player1Ready = match.Player1?.IsReady ?? false,
        Player2Ready = match.Player2?.IsReady ?? false,
        Player1MissedRounds = match.Player1?.MissedRounds ?? 0,
        Player2MissedRounds = match.Player2?.MissedRounds ?? 0,
        Player1Kingdom = match.Player1?.Kingdom.ToKingdomDto(),
        Player2Kingdom = match.Player2?.Kingdom.ToKingdomDto(),
        Battle = match.LiveBattle?.ToBattleDto(match.SideOf(userId), DateTimeOffset.UtcNow),
        March = match.MarchChoice?.ToMarchDto(match.SideOf(userId), DateTimeOffset.UtcNow),
    };

    private static MarchDto ToMarchDto(this MarchChoice choice, int side, DateTimeOffset now) => new()
    {
        Side = choice.Side,
        Army = choice.Army,
        SecondsRemaining = Math.Round(choice.SecondsLeftAt(now), 1),
        IsYours = choice.Side == side,
    };

    private static BattleDto ToBattleDto(this LiveBattle battle, int side, DateTimeOffset now)
    {
        var replay = battle.Replay;

        var shown = Math.Min(battle.ElapsedAt(now), battle.EndsAt);
        var snapshot = replay.At(shown);

        return new BattleDto
        {
            AttackerSide = replay.AttackerSide,
            Phase = snapshot.Phase,
            Player1Start = replay.Player1Start,
            Player2Start = replay.Player2Start,
            Player1Left = snapshot.Player1Left,
            Player2Left = snapshot.Player2Left,
            Elapsed = Math.Round(shown, 2),
            Duration = battle.EndsAt,
            BreachAt = replay.BreachAt,
            Over = battle.IsOverAt(now),
            RetreatBySide = battle.Retreat?.Side,
            RetreatAt = battle.Retreat?.At,
            CanRetreat = battle.CanRetreat(side) && !battle.IsOverAt(now),
            Falls = replay.FallsUntil(shown)
                .Select(fall => new BattleFallDto { At = fall.At, Side = fall.Side, Phase = fall.Phase })
                .ToList(),
        };
    }

    public static int? SecondsRemaining(this Match match)
    {
        if (match.Status != MatchStatus.InProgress || match.RoundDeadline is not { } deadline)
        {
            return null;
        }

        var left = (deadline - DateTimeOffset.UtcNow).TotalSeconds;

        return left <= 0 ? 0 : (int)Math.Ceiling(left);
    }

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

    private static KingdomDto ToKingdomDto(this KingdomState kingdom) => new()
    {
        Gold = kingdom.Gold,
        Troops = kingdom.Troops,
        Morale = kingdom.Morale,
        TotalPower = kingdom.TotalPower,
    };
}
