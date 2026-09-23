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
        Player1Kingdom = match.KingdomSeenBy(userId, 1),
        Player2Kingdom = match.KingdomSeenBy(userId, 2),
        YourCaravan = match.SlotOf(userId)?.Caravan is { } caravan
            ? new CaravanDto { Troops = caravan.Troops, ReturnsInRound = caravan.AvailableInRound }
            : null,
        Battle = match.LiveBattle?.ToBattleDto(match.SideOf(userId), DateTimeOffset.UtcNow),
        March = match.MarchChoice?.ToMarchDto(match.SideOf(userId), DateTimeOffset.UtcNow),
        LastRound = match.LastRoundSeenBy(userId),
        Story = match.StoryOf(),
    };

    private static RoundRecapDto? LastRoundSeenBy(this Match match, Guid userId)
    {
        var side = match.SideOf(userId);

        if (side == 0 || match.History.Count == 0)
        {
            return null;
        }

        var last = match.History[^1];
        var after = side == 1 ? last.Player1.After : last.Player2.After;
        var before = match.History.Count > 1
            ? (side == 1 ? match.History[^2].Player1.After : match.History[^2].Player2.After)
            : KingdomState.Start(match.StartingGold, match.StartingTroops, match.StartingMorale);

        return new RoundRecapDto
        {
            Round = last.RoundNumber,
            GoldDelta = after.Gold - before.Gold,
            TroopsDelta = after.Troops - before.Troops,
            MoraleDelta = after.Morale - before.Morale,
            Lines = last.Events
                .Where(happening => happening.Side == side)
                .Select(happening => new RoundLineDto
                {
                    Kind = happening.Kind,
                    Amount = happening.Amount,
                    Direction = happening.Direction,
                    Purchase = happening.Purchase,
                })
                .ToList(),
        };
    }

    private static readonly RoundEventKind[] Turns =
    [
        RoundEventKind.BattleJoined,
        RoundEventKind.WallsBreached,
        RoundEventKind.WallsHeld,
        RoundEventKind.Retreated,
        RoundEventKind.MarchedOn,
        RoundEventKind.CastleDestroyed,
    ];

    private static MatchStoryDto? StoryOf(this Match match)
    {
        if (match.Status != MatchStatus.Completed)
        {
            return null;
        }

        var start = KingdomState.Start(match.StartingGold, match.StartingTroops, match.StartingMorale);
        var rounds = new List<StoryRoundDto>
        {
            new() { Round = 0, Player1Power = start.TotalPower, Player2Power = start.TotalPower },
        };

        rounds.AddRange(match.History.Select(round => new StoryRoundDto
        {
            Round = round.RoundNumber,
            Player1Power = round.Player1.After.TotalPower,
            Player2Power = round.Player2.After.TotalPower,
            Moments = round.Events
                .Where(happening => Turns.Contains(happening.Kind))
                .Select(happening => new StoryMomentDto
                {
                    Kind = happening.Kind,
                    Side = happening.Side,
                    Amount = happening.Amount,
                })
                .ToList(),
        }));

        return new MatchStoryDto { Rounds = rounds };
    }
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
            KeepDefenders = replay.BreachAt is { } breach && shown >= breach ? replay.DefenderInKeep : null,
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

    private static KingdomDto? KingdomSeenBy(this Match match, Guid userId, int side)
    {
        var slot = side == 1 ? match.Player1 : match.Player2;

        if (slot is null)
        {
            return null;
        }

        return match.Status == MatchStatus.InProgress && match.SideOf(userId) != side
            ? null
            : slot.Kingdom.ToKingdomDto();
    }

    private static KingdomDto ToKingdomDto(this KingdomState kingdom) => new()
    {
        Gold = kingdom.Gold,
        Troops = kingdom.Troops,
        Morale = kingdom.Morale,
        TotalPower = kingdom.TotalPower,
        Fortification = kingdom.Fortification,
        Catapults = kingdom.Catapults,
    };
}
