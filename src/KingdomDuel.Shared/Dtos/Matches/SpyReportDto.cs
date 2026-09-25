using KingdomDuel.Domain.Enums;

namespace KingdomDuel.Shared.Dtos.Matches;

public sealed class SpyOrdersDto
{
    public FeastTier? Feast { get; set; }
    public int Defenders { get; set; }
    public bool Recruited { get; set; }
    public bool TouchedSpies { get; set; }
}

public sealed class SpyReportDto
{
    public int Round { get; set; }
    public SpyTier Tier { get; set; }
    public double Accuracy { get; set; }
    public bool CaravanSent { get; set; }
    public int CaravanVillagers { get; set; }
    public int Fortification { get; set; }
    public int Villagers { get; set; }
    public int Troops { get; set; }
    public int Morale { get; set; }
    public SpyOrdersDto? Orders { get; set; }
}
