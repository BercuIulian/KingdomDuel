using FluentValidation;
using KingdomDuel.Shared.Requests.Auth;

namespace KingdomDuel.Shared.Validators.Auth;

public sealed class ChangePasswordValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty()
            .WithMessage("Parola curentă este obligatorie.")
            .MaximumLength(AuthRules.PasswordMaxLength)
            .WithMessage($"Parola nu poate depăși {AuthRules.PasswordMaxLength} de caractere.");

        RuleFor(x => x.NewPassword)
            .NewPassword()
            .NotEqual(x => x.CurrentPassword)
            .WithMessage("Parola nouă trebuie să fie diferită de cea curentă.");
    }
}
