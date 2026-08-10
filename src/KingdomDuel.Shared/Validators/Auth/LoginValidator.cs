using FluentValidation;
using KingdomDuel.Shared.Requests.Auth;

namespace KingdomDuel.Shared.Validators.Auth;

public sealed class LoginValidator : AbstractValidator<LoginRequest>
{
    public LoginValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty()
            .WithMessage("Numele de utilizator este obligatoriu.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Parola este obligatorie.")
            .MaximumLength(AuthRules.PasswordMaxLength)
            .WithMessage($"Parola nu poate depăși {AuthRules.PasswordMaxLength} de caractere.");
    }
}
