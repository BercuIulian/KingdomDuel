using FluentValidation;
using KingdomDuel.Domain;
using KingdomDuel.Shared.Requests.Matches;

namespace KingdomDuel.Shared.Validators.Matches;

public sealed class UpdateLobbyValidator : AbstractValidator<UpdateLobbyRequest>
{
    public UpdateLobbyValidator()
    {
        RuleFor(x => x.TotalRounds)
            .InclusiveBetween(GameConstants.MinTotalRounds, GameConstants.MaxTotalRounds)
            .WithMessage($"Numărul de runde trebuie să fie între {GameConstants.MinTotalRounds} și {GameConstants.MaxTotalRounds}.");
        RuleFor(x => x.StartingGold).InclusiveBetween(0, 500);

        RuleFor(x => x.StartingTroops)
            .InclusiveBetween(1, 500)
            .WithMessage("Trupele de start trebuie să fie între 1 și 500.");

        RuleFor(x => x.StartingMorale)
            .InclusiveBetween(1, 100)
            .WithMessage("Moralul de start trebuie să fie între 1 și 100.");

        RuleFor(x => x.RoundSeconds)
            .InclusiveBetween(GameConstants.MinRoundSeconds, GameConstants.MaxRoundSeconds)
            .WithMessage($"Timpul pe rundă trebuie să fie între {GameConstants.MinRoundSeconds} și {GameConstants.MaxRoundSeconds} de secunde.");
    }
}
