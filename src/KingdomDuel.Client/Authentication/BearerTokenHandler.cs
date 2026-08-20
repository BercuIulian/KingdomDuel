using System.Net;
using System.Net.Http.Headers;
using KingdomDuel.Shared.Auth;
using Microsoft.AspNetCore.Components;

namespace KingdomDuel.Client.Authentication;

public sealed class BearerTokenHandler(
    TokenStore tokens,
    TokenAuthenticationStateProvider authentication,
    NavigationManager navigation) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = await tokens.GetAsync();

        if (token is not null && !JwtPayload.IsExpired(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await base.SendAsync(request, cancellationToken);

        await AdoptRenewedTokenAsync(response);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await SignOutAndRedirectAsync();
        }

        return response;
    }

    private async Task AdoptRenewedTokenAsync(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues(AuthHeaders.RenewedToken, out var values))
        {
            return;
        }

        var renewed = values.FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(renewed))
        {
            await authentication.SignInAsync(renewed);
        }
    }

    private async Task SignOutAndRedirectAsync()
    {
        await authentication.SignOutAsync();

        var current = navigation.ToBaseRelativePath(navigation.Uri);

        if (Routes.IsLogin(current))
        {
            return;
        }

        navigation.NavigateTo(Routes.LoginWithReturnUrl(current));
    }
}
