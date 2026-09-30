using KingdomDuel.Application.Features.Matches;
using KingdomDuel.Domain;
using KingdomDuel.Domain.Entities.Matches;
using KingdomDuel.Domain.Entities.Users;
using KingdomDuel.Domain.Enums;
using KingdomDuel.Domain.Rounds;
using Microsoft.Extensions.Time.Testing;
using static KingdomDuel.UnitTests.Domain.GameSetup;

namespace KingdomDuel.UnitTests.Application;

internal sealed class MatchSetup
{
    public static readonly Guid Host = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid Guest = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid Stranger = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public static readonly DateTimeOffset Start = new(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);

    public FakeTimeProvider Time { get; } = new(Start);

    public DateTimeOffset Now => Time.GetUtcNow();

    public RoundResolver Resolver(Random? random = null) => new(Time, random ?? new Random(1));

    public void Wait(double seconds) => Time.Advance(TimeSpan.FromSeconds(seconds));

    public Match InProgress(
        KingdomState? player1 = null,
        KingdomState? player2 = null,
        int round = 1,
        int totalRounds = 10) => new()
    {
        Code = "DUEL42",
        Status = MatchStatus.InProgress,
        Round = round,
        TotalRounds = totalRounds,
        StartedAt = Start,
        RoundDeadline = Now.AddSeconds(GameConstants.DefaultRoundSeconds),
        Player1 = Slot(Host, "gazda", player1 ?? Kingdom()),
        Player2 = Slot(Guest, "oaspete", player2 ?? Kingdom()),
    };

    public static PlayerSlot Slot(Guid userId, string userName, KingdomState kingdom) => new()
    {
        UserId = userId,
        User = new User { Id = userId, UserName = userName, PasswordHash = "x" },
        Kingdom = kingdom,
    };

    public static void Orders(Match match, OrderPlan? player1, OrderPlan? player2)
    {
        match.Player1!.PendingOrder = player1;
        match.Player2!.PendingOrder = player2;
    }
}
