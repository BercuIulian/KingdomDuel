using KingdomDuel.Client.ApiClients;
using KingdomDuel.Shared.Requests.Auth;
using Microsoft.AspNetCore.Components;

namespace KingdomDuel.Client.Authentication;

public sealed class AuthenticationService(
    ApiClient api,
    TokenAuthenticationStateProvider authentication,
    NavigationManager navigation)
{
    private const string GenericCredentialsError = "Nume de utilizator sau parolă greșite.";
    private const string ServerUnreachable = "Nu am putut contacta serverul.";

    public Task<string?> LoginAsync(LoginRequest request)
        => SignInAsync(() => api.LoginAsync(request), GenericCredentialsError);

    public Task<string?> RegisterAsync(RegisterRequest request)
        => SignInAsync(() => api.RegisterAsync(request), "Contul nu a putut fi creat.");

    public async Task LogoutAsync()
    {
        try
        {
            await api.LogoutAsync();
        }
        catch (Exception)
        {
            // The local token goes regardless: a server that cannot be reached must not
            // leave the user looking signed in.
        }

        await authentication.SignOutAsync();

        navigation.NavigateTo($"/{LoginRoute.Path}");
    }

    private async Task<string?> SignInAsync(Func<Task<Shared.Responses.Auth.AuthResponse>> call, string fallback)
    {
        try
        {
            var response = await call();

            await authentication.SignInAsync(response.Token);

            return null;
        }
        catch (ApiException exception)
        {
            return ApiErrorReader.Describe(exception, fallback);
        }
        catch (Exception)
        {
            return ServerUnreachable;
        }
    }
}
