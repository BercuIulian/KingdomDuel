namespace KingdomDuel.Domain.Rounds;

public sealed record MarchChoice(int Side, int Army, DateTimeOffset Until, RoundOutcome AfterField)
{
    public double SecondsLeftAt(DateTimeOffset now) => Math.Max(0, (Until - now).TotalSeconds);

    public bool IsDueAt(DateTimeOffset now) => now >= Until;
}
