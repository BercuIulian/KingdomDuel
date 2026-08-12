using Microsoft.JSInterop;

namespace KingdomDuel.Client.Authentication;

public sealed class TokenStore(IJSRuntime js)
{
    private const string StorageKey = "kingdomduel.token";

    private string? cached;
    private bool loaded;

    public async ValueTask<string?> GetAsync()
    {
        if (!loaded)
        {
            cached = await js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
            loaded = true;
        }

        return cached;
    }

    public async ValueTask SetAsync(string token)
    {
        cached = token;
        loaded = true;
        await js.InvokeVoidAsync("localStorage.setItem", StorageKey, token);
    }

    public async ValueTask ClearAsync()
    {
        cached = null;
        loaded = true;
        await js.InvokeVoidAsync("localStorage.removeItem", StorageKey);
    }
}
