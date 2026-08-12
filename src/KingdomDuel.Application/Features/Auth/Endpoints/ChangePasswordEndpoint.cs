using FastEndpoints;
using KingdomDuel.Application.Features.Auth.Services;
using KingdomDuel.Shared.Requests.Auth;
using KingdomDuel.Shared.Validators.Auth;
using Microsoft.AspNetCore.Builder;

namespace KingdomDuel.Application.Features.Auth.Endpoints;

internal sealed class ChangePasswordEndpoint(UserProfileEditor editor) : Endpoint<ChangePasswordRequest>
{
    public override void Configure()
    {
        Put("/auth/profile/password");
        Validator<ChangePasswordValidator>();
        Description(b => b.WithName("ChangePassword"));
    }

    public override async Task HandleAsync(ChangePasswordRequest req, CancellationToken ct)
    {
        var changed = await editor.ChangePasswordAsync(
            User.GetUserId(),
            req.CurrentPassword,
            req.NewPassword,
            ct);

        if (!changed)
        {
            AddError(r => r.CurrentPassword, AuthErrors.IncorrectPassword);
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}
