namespace KingdomDuel.Domain.Rounds;

public sealed record Retreat(int Side, double At);

public sealed record LiveBattle(
    BattleReplay Replay,
    RoundOutcome BeforeBattle,
    DateTimeOffset StartedAt,
    Retreat? Retreat = null)
{
    public double EndsAt => Retreat is { } signal
        ? Math.Min(signal.At + GameConstants.RetreatSeconds, Replay.Duration)
        : Replay.Duration;

    public double ElapsedAt(DateTimeOffset now) => Math.Max(0, (now - StartedAt).TotalSeconds);

    public bool IsOverAt(DateTimeOffset now) => ElapsedAt(now) >= EndsAt;

    public bool CanRetreat(int side) =>
        Retreat is null && side is 1 or 2 && (Replay.AttackerSide == 0 || Replay.AttackerSide == side);
}
