namespace KingdomDuel.Shared.Dtos.Matches;

public sealed class MarchDto
{
    public int Side { get; set; }
    public int Army { get; set; }
    public double SecondsRemaining { get; set; }
    public bool IsYours { get; set; }
}
