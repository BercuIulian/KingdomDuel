using FluentValidation;

namespace KingdomDuel.Shared.Validators.Auth;

public static class AuthValidation
{
    public static IRuleBuilderOptions<T, string> UserName<T>(this IRuleBuilder<T, string> rule) => rule
        .NotEmpty()
        .WithMessage("Numele de utilizator este obligatoriu.")
        .MinimumLength(AuthRules.UserNameMinLength)
        .WithMessage($"Numele de utilizator trebuie să aibă cel puțin {AuthRules.UserNameMinLength} caractere.")
        .MaximumLength(AuthRules.UserNameMaxLength)
        .WithMessage($"Numele de utilizator nu poate depăși {AuthRules.UserNameMaxLength} de caractere.")
        .Matches(AuthRules.UserNamePattern)
        .WithMessage(AuthRules.UserNamePatternMessage);

    public static IRuleBuilderOptions<T, string> NewPassword<T>(this IRuleBuilder<T, string> rule) => rule
        .NotEmpty()
        .WithMessage("Parola este obligatorie.")
        .MinimumLength(AuthRules.PasswordMinLength)
        .WithMessage($"Parola trebuie să aibă cel puțin {AuthRules.PasswordMinLength} caractere.")
        .MaximumLength(AuthRules.PasswordMaxLength)
        .WithMessage($"Parola nu poate depăși {AuthRules.PasswordMaxLength} de caractere.");

    public static IRuleBuilderOptions<T, string?> Email<T>(this IRuleBuilder<T, string?> rule) => rule
        .EmailAddress()
        .WithMessage("Adresa de email nu este validă.")
        .MaximumLength(AuthRules.EmailMaxLength)
        .WithMessage($"Adresa de email nu poate depăși {AuthRules.EmailMaxLength} de caractere.");
}
