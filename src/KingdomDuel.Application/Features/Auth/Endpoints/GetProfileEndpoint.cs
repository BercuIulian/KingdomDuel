using FastEndpoints;
using KingdomDuel.Shared.Dtos.Auth;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;

namespace KingdomDuel.Application.Features.Auth.Endpoints;

internal sealed class GetProfileEndpoint(AppDbContext dbContext)
    : EndpointWithoutRequest<UserProfileDto>
{
    public override void Configure()
    {
        Get("/auth/profile");
        Description(b => b.WithName("GetProfile"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.GetUserId();

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        await Send.OkAsync(user.ToProfileDto(), ct);
    }
}
