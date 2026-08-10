using FluentValidation;
using KingdomDuel.Shared.Requests.Auth;

namespace KingdomDuel.Shared.Validators.Auth;

public sealed class RegisterValidator : AbstractValidator<RegisterRequest>
{
    public RegisterValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty()
            .WithMessage("Numele de utilizator este obligatoriu.")
            .MinimumLength(AuthRules.UserNameMinLength)
            .WithMessage($"Numele de utilizator trebuie să aibă cel puțin {AuthRules.UserNameMinLength} caractere.")
            .MaximumLength(AuthRules.UserNameMaxLength)
            .WithMessage($"Numele de utilizator nu poate depăși {AuthRules.UserNameMaxLength} de caractere.")
            .Matches(AuthRules.UserNamePattern)
            .WithMessage(AuthRules.UserNamePatternMessage);

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Parola este obligatorie.")
            .MinimumLength(AuthRules.PasswordMinLength)
            .WithMessage($"Parola trebuie să aibă cel puțin {AuthRules.PasswordMinLength} caractere.")
            .MaximumLength(AuthRules.PasswordMaxLength)
            .WithMessage($"Parola nu poate depăși {AuthRules.PasswordMaxLength} de caractere.");

        RuleFor(x => x.Email)
            .EmailAddress()
            .WithMessage("Adresa de email nu este validă.")
            .MaximumLength(AuthRules.EmailMaxLength)
            .WithMessage($"Adresa de email nu poate depăși {AuthRules.EmailMaxLength} de caractere.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}
