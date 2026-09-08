namespace KingdomDuel.Domain.Rounds;

public sealed record LiveBattle(BattleReplay Replay, RoundOutcome Outcome, DateTimeOffset StartedAt)
{
    public double ElapsedAt(DateTimeOffset now) => Math.Max(0, (now - StartedAt).TotalSeconds);

    public bool IsOverAt(DateTimeOffset now) => ElapsedAt(now) >= Replay.Duration;
}
