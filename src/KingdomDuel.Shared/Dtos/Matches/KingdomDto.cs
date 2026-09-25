namespace KingdomDuel.Shared.Dtos.Matches;

public sealed class KingdomDto
{
    public int Gold { get; set; }
    public int Troops { get; set; }
    public int Villagers { get; set; }
    public int Morale { get; set; }
    public int TotalPower { get; set; }
    public int Fortification { get; set; }
    public int Catapults { get; set; }
    public int RuinedFrom { get; set; }
    public bool IsHearsay { get; set; }
}
