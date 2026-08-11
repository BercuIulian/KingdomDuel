using FastEndpoints;
using KingdomDuel.Application.Features.Auth.Services;
using KingdomDuel.Shared.Requests.Auth;
using KingdomDuel.Shared.Responses.Auth;
using KingdomDuel.Shared.Validators.Auth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.RateLimiting;

namespace KingdomDuel.Application.Features.Auth.Endpoints;

internal sealed class LoginEndpoint(UserAuthenticator authenticator, TokenIssuer tokenIssuer)
    : Endpoint<LoginRequest, AuthResponse>
{
    public override void Configure()
    {
        Post("/auth/login");
        AllowAnonymous();
        Validator<LoginValidator>();
        Options(x => x.RequireRateLimiting(RateLimitPolicies.Login));
        Description(b => b.WithName("Login"));
    }

    public override async Task HandleAsync(LoginRequest req, CancellationToken ct)
    {
        var user = await authenticator.AuthenticateAsync(req.UserName, req.Password, ct);

        if (user is null)
        {
            AddError(AuthErrors.InvalidCredentials);
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        await Send.OkAsync(user.ToAuthResponse(tokenIssuer.Issue(user)), ct);
    }
}
