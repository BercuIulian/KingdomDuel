namespace KingdomDuel.Domain.Rounds;

public sealed record BreachCall(int Side, int Most, DateTimeOffset Until)
{
    public double SecondsLeftAt(DateTimeOffset now) => Math.Max(0, (Until - now).TotalSeconds);

    public bool IsDueAt(DateTimeOffset now) => now >= Until;
}
