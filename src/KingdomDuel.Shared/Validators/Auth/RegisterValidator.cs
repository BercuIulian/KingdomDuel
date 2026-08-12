using FluentValidation;
using KingdomDuel.Shared.Requests.Auth;

namespace KingdomDuel.Shared.Validators.Auth;

public sealed class RegisterValidator : AbstractValidator<RegisterRequest>
{
    public RegisterValidator()
    {
        RuleFor(x => x.UserName).UserName();

        RuleFor(x => x.Password).NewPassword();

        RuleFor(x => x.Email)
            .Email()
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}
