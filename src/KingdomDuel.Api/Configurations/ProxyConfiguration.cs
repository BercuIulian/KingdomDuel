using Microsoft.AspNetCore.HttpOverrides;

namespace KingdomDuel.Api.Configurations;

public static class ProxyConfiguration
{
    public static WebApplicationBuilder AddProxyConfiguration(this WebApplicationBuilder builder)
    {
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });

        return builder;
    }
}
