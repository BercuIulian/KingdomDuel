using FluentValidation;
using KingdomDuel.Shared.Requests.Matches;

namespace KingdomDuel.Shared.Validators.Matches;

public sealed class SubmitOrderValidator : AbstractValidator<SubmitOrderRequest>
{
    public SubmitOrderValidator()
    {
        RuleForEach(x => x.Troops).ChildRules(allocation =>
        {
            allocation.RuleFor(a => a.Direction)
                .IsInEnum()
                .WithMessage("Direcția nu există.");

            allocation.RuleFor(a => a.Troops)
                .GreaterThan(0)
                .WithMessage("O direcție primește cel puțin un soldat sau nu apare deloc.");
        });

        RuleFor(x => x.Troops)
            .Must(troops => troops.Select(a => a.Direction).Distinct().Count() == troops.Count)
            .WithMessage("Aceeași direcție apare de două ori.");
    }
}
