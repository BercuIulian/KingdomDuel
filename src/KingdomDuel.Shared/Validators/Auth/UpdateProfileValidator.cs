using FluentValidation;
using KingdomDuel.Shared.Requests.Auth;

namespace KingdomDuel.Shared.Validators.Auth;

public sealed class UpdateProfileValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileValidator()
    {
        RuleFor(x => x.UserName).UserName();

        RuleFor(x => x.Email)
            .Email()
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}
