using KingdomDuel.Application.Features.Auth.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;

namespace KingdomDuel.Application.Features.Auth;

public static class StaleTokenRejection
{
    public static async Task RejectStaleTokenAsync(this TokenValidatedContext context)
    {
        var validator = context.HttpContext.RequestServices.GetRequiredService<TokenVersionValidator>();

        if (!await validator.IsCurrentAsync(context.Principal!, context.HttpContext.RequestAborted))
        {
            context.Fail("Token revocat.");
        }
    }
}
