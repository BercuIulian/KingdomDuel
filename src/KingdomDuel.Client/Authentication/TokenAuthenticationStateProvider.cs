using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace KingdomDuel.Client.Authentication;

public sealed class TokenAuthenticationStateProvider(TokenStore tokens) : AuthenticationStateProvider
{
    private static readonly AuthenticationState Anonymous = new(new ClaimsPrincipal(new ClaimsIdentity()));

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await tokens.GetAsync();

        if (token is null || JwtPayload.IsExpired(token))
        {
            return Anonymous;
        }

        return Authenticated(token);
    }

    public async Task SignInAsync(string token)
    {
        await tokens.SetAsync(token);

        NotifyAuthenticationStateChanged(Task.FromResult(Authenticated(token)));
    }

    public async Task SignOutAsync()
    {
        await tokens.ClearAsync();

        NotifyAuthenticationStateChanged(Task.FromResult(Anonymous));
    }

    private static AuthenticationState Authenticated(string token)
    {
        var identity = new ClaimsIdentity(JwtPayload.ReadClaims(token), "jwt", JwtPayload.NameClaim, ClaimTypes.Role);

        return new AuthenticationState(new ClaimsPrincipal(identity));
    }
}
