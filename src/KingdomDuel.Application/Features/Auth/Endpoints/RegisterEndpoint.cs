using FastEndpoints;
using KingdomDuel.Application.Features.Auth.Services;
using KingdomDuel.Shared.Requests.Auth;
using KingdomDuel.Shared.Responses.Auth;
using KingdomDuel.Shared.Validators.Auth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.RateLimiting;

namespace KingdomDuel.Application.Features.Auth.Endpoints;

internal sealed class RegisterEndpoint(UserRegistrar registrar, TokenIssuer tokenIssuer)
    : Endpoint<RegisterRequest, AuthResponse>
{
    public override void Configure()
    {
        Post("/auth/register");
        AllowAnonymous();
        Validator<RegisterValidator>();
        Options(x => x.RequireRateLimiting(RateLimitPolicies.Register));
        Description(b => b.WithName("Register"));
    }

    public override async Task HandleAsync(RegisterRequest req, CancellationToken ct)
    {
        var user = await registrar.RegisterAsync(req.UserName, req.Email, req.Password, ct);

        if (user is null)
        {
            AddError(r => r.UserName, AuthErrors.UserNameTaken);
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        await Send.OkAsync(user.ToAuthResponse(tokenIssuer.Issue(user)), ct);
    }
}
