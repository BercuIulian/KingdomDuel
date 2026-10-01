namespace KingdomDuel.Shared.Dtos.Matches;

public sealed class RematchDto
{
    public bool Available { get; set; }
    public bool YouAsked { get; set; }
    public bool OpponentAsked { get; set; }
    public bool OpponentGone { get; set; }
    public string? Code { get; set; }
    public double? SecondsRemaining { get; set; }
}
