namespace KingdomDuel.Shared.Dtos.Matches;

public sealed class MatchHistoryDto
{
    public int Played { get; set; }
    public int Won { get; set; }
    public int Lost { get; set; }
    public int Draw { get; set; }

    public List<MatchHistoryEntryDto> Entries { get; set; } = [];
}
