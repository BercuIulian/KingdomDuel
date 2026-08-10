using FastEndpoints;
using KingdomDuel.Application.Features.Auth.Services;
using Microsoft.AspNetCore.Builder;

namespace KingdomDuel.Application.Features.Auth.Endpoints;

internal sealed class LogoutEndpoint(TokenRevoker revoker) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Post("/auth/logout");
        Description(b => b.WithName("Logout"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await revoker.RevokeAllAsync(User.GetUserId(), ct);

        await Send.NoContentAsync(ct);
    }
}
