using KingdomDuel.Domain.Rounds;

namespace KingdomDuel.Shared.Dtos.Matches;

public sealed class MatchStoryDto
{
    public List<StoryRoundDto> Rounds { get; set; } = [];
}

public sealed class StoryRoundDto
{
    public int Round { get; set; }
    public int Player1Power { get; set; }
    public int Player2Power { get; set; }
    public List<StoryMomentDto> Moments { get; set; } = [];
}

public sealed class StoryMomentDto
{
    public RoundEventKind Kind { get; set; }
    public int Side { get; set; }
    public int Amount { get; set; }
}
