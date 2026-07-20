using FluentValidation;
using KingdomDuel.Shared.Requests.Matches;

namespace KingdomDuel.Shared.Validators.Matches;

public sealed class JoinMatchValidator : AbstractValidator<JoinMatchRequest>
{
    public JoinMatchValidator()
    {
        RuleFor(x => x.DisplayName)
            .Must(name => !string.IsNullOrWhiteSpace(name))
            .WithMessage("Numele jucătorului este obligatoriu.");
    }
}
