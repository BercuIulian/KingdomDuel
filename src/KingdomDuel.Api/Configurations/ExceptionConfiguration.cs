using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;

namespace KingdomDuel.Api.Configurations;

public static class ExceptionConfiguration
{
    private const string Message = "Ceva a mers prost pe server. Încearcă din nou peste o clipă.";

    public static void UseExceptionConfiguration(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            return;
        }

        app.UseExceptionHandler(new ExceptionHandlerOptions { ExceptionHandler = WriteAsync });
    }

    private static async Task WriteAsync(HttpContext context)
    {
        var error = context.Features.Get<IExceptionHandlerFeature>()?.Error;

        context.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("KingdomDuel.UnhandledException")
            .LogError(error, "Cerere eșuată: {Method} {Path}", context.Request.Method, context.Request.Path);

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(new
            {
                statusCode = StatusCodes.Status500InternalServerError,
                message = Message,
            }),
            context.RequestAborted);
    }
}
