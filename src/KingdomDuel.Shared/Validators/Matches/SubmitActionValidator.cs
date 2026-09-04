using FluentValidation;
using KingdomDuel.Domain;
using KingdomDuel.Domain.Enums;
using KingdomDuel.Shared.Requests.Matches;

namespace KingdomDuel.Shared.Validators.Matches;

public sealed class SubmitActionValidator : AbstractValidator<SubmitActionRequest>
{
    public SubmitActionValidator()
    {
        // Plafonul rundei depinde de eveniment și de oastea de acasă, deci aici
        // verificăm doar că numărul e în domeniul posibil; taierea la realitate o
        // face motorul, care vede starea din clipa rezolvării.
        RuleFor(x => x.Troops)
            .InclusiveBetween(GameConstants.MinAttackTroops, GameConstants.MaxAttackTroops)
            .When(x => x.Action == ActionType.Attack)
            .WithMessage($"Într-o expediție pleacă între {GameConstants.MinAttackTroops} și {GameConstants.MaxAttackTroops} trupe.");
    }
}
