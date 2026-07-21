using FluentValidation;
using KingdomDuel.Shared.Requests.Matches;

namespace KingdomDuel.Shared.Validators.Matches;

public sealed class UpdateLobbyValidator : AbstractValidator<UpdateLobbyRequest>
{
    public UpdateLobbyValidator()
    {
        RuleFor(x => x.PlayerId)
            .Must(id => !string.IsNullOrWhiteSpace(id))
            .WithMessage("Identificatorul jucătorului este obligatoriu.");

        RuleFor(x => x.DisplayName)
            .Must(name => !string.IsNullOrWhiteSpace(name))
            .WithMessage("Numele jucătorului este obligatoriu.")
            .MaximumLength(24)
            .WithMessage("Numele nu poate depăși 24 de caractere.");

        RuleFor(x => x.TotalRounds).InclusiveBetween(1, 20);
        RuleFor(x => x.StartingGold).InclusiveBetween(0, 500);
        RuleFor(x => x.StartingTroops).InclusiveBetween(0, 500);
        RuleFor(x => x.StartingMorale).InclusiveBetween(0, 100);
    }
}
