using FastEndpoints;
using KingdomDuel.Application.Features.Auth.Services;
using KingdomDuel.Shared.Requests.Auth;
using KingdomDuel.Shared.Responses.Auth;
using KingdomDuel.Shared.Validators.Auth;
using Microsoft.AspNetCore.Builder;

namespace KingdomDuel.Application.Features.Auth.Endpoints;

internal sealed class UpdateProfileEndpoint(UserProfileEditor editor)
    : Endpoint<UpdateProfileRequest, ProfileUpdateResponse>
{
    public override void Configure()
    {
        Put("/auth/profile");
        Validator<UpdateProfileValidator>();
        Description(b => b.WithName("UpdateProfile"));
    }

    public override async Task HandleAsync(UpdateProfileRequest req, CancellationToken ct)
    {
        var update = await editor.UpdateAsync(User.GetUserId(), req.UserName, req.Email, ct);

        if (update is null)
        {
            AddError(r => r.UserName, AuthErrors.UserNameTaken);
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        await Send.OkAsync(
            new ProfileUpdateResponse
            {
                User = update.User.ToProfileDto(),
                RequiresSignIn = update.RequiresSignIn,
            },
            ct);
    }
}
