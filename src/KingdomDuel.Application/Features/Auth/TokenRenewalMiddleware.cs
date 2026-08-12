using KingdomDuel.Application.Features.Auth.Services;
using KingdomDuel.Shared.Auth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace KingdomDuel.Application.Features.Auth;

internal sealed class TokenRenewalMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context, TokenRenewer renewer)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var renewed = renewer.RenewIfNearExpiry(context.User);

            if (renewed is not null)
            {
                context.Response.Headers[AuthHeaders.RenewedToken] = renewed.Value;
            }
        }

        return next(context);
    }
}

public static class TokenRenewalMiddlewareExtensions
{
    public static IApplicationBuilder UseTokenRenewal(this IApplicationBuilder app)
        => app.UseMiddleware<TokenRenewalMiddleware>();
}
