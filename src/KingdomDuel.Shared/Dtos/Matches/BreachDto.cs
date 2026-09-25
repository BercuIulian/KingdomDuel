namespace KingdomDuel.Shared.Dtos.Matches;

public sealed class BreachDto
{
    public int Side { get; set; }
    public int Most { get; set; }
    public double SecondsRemaining { get; set; }
    public bool IsYours { get; set; }
}
