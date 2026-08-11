using System.Globalization;
using System.Text.Json;
using System.Threading.RateLimiting;
using KingdomDuel.Application;
using KingdomDuel.Application.Features.Auth;
using Microsoft.AspNetCore.RateLimiting;

namespace KingdomDuel.Api.Configurations;

public static class RateLimitConfiguration
{
    private const string GenericMessage = "Prea multe cereri. Încetinește puțin și încearcă din nou.";

    public static WebApplicationBuilder AddRateLimitConfiguration(this WebApplicationBuilder builder)
    {
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = WriteRejectionAsync;

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                context => FixedWindow(CallerKey(context), permitLimit: 90, window: TimeSpan.FromMinutes(1)));

            options.AddPolicy(
                RateLimitPolicies.CreateMatch,
                context => FixedWindow(CallerKey(context), permitLimit: 20, window: TimeSpan.FromHours(1)));

            options.AddPolicy(
                RateLimitPolicies.Login,
                context => FixedWindow(IpKey(context), permitLimit: 10, window: TimeSpan.FromMinutes(1)));

            options.AddPolicy(
                RateLimitPolicies.Register,
                context => FixedWindow(IpKey(context), permitLimit: 5, window: TimeSpan.FromHours(1)));
        });

        return builder;
    }

    private static RateLimitPartition<string> FixedWindow(string key, int permitLimit, TimeSpan window)
        => RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = window,
            QueueLimit = 0,
            AutoReplenishment = true,
        });

    private static string CallerKey(HttpContext context)
        => context.User.FindUserId()?.ToString() ?? IpKey(context);

    private static string IpKey(HttpContext context)
        => context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    private static async ValueTask WriteRejectionAsync(OnRejectedContext context, CancellationToken ct)
    {
        var response = context.HttpContext.Response;

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            response.Headers.RetryAfter =
                ((int)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        }

        response.StatusCode = StatusCodes.Status429TooManyRequests;
        response.ContentType = "application/json";

        await response.WriteAsync(
            JsonSerializer.Serialize(new
            {
                statusCode = StatusCodes.Status429TooManyRequests,
                message = MessageFor(context.HttpContext),
            }),
            ct);
    }

    private static string MessageFor(HttpContext context)
    {
        var policy = context.GetEndpoint()?.Metadata
            .GetMetadata<EnableRateLimitingAttribute>()?.PolicyName;

        return policy switch
        {
            RateLimitPolicies.CreateMatch =>
                "Ai creat prea multe meciuri în ultima oră. Încearcă din nou mai târziu.",
            RateLimitPolicies.Login =>
                "Prea multe încercări de autentificare. Așteaptă un minut și încearcă din nou.",
            RateLimitPolicies.Register =>
                "Prea multe conturi create de pe această rețea. Încearcă din nou mai târziu.",
            _ => GenericMessage,
        };
    }
}
