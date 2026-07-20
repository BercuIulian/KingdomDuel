using FastEndpoints.ClientGen;
using NSwag.CodeGeneration.OperationNameGenerators;

namespace KingdomDuel.Api.Configurations;

/// <summary>
/// Generates the typed C# API client consumed by the Blazor UI, mirroring the
/// eAdmitere setup: NSwag's CSharpClientGenerator over the FastEndpoints OpenAPI
/// document, with GenerateDtoTypes=false so the client REUSES the DTO/request/
/// response/enum types from KingdomDuel.Shared instead of regenerating them.
///
/// Run from the API project directory:  dotnet run --generateclients true
/// </summary>
internal static class ClientGenerationConfiguration
{
    /// <summary>Must match the SwaggerDocument name registered in ApiConfiguration.</summary>
    public const string DocumentName = "Kingdom Duel API";

    /// <summary>
    /// Every namespace that holds a type referenced by the client. Because DTO types
    /// are not regenerated, the client needs a `using` for each of these to resolve
    /// the real shared types at compile time. Add new ones here as the API grows.
    /// </summary>
    private static readonly string[] SharedNamespaceUsages =
    [
        "KingdomDuel.Shared.Dtos.Matches",
        "KingdomDuel.Shared.Requests.Matches",
        "KingdomDuel.Shared.Responses.Matches",
        "KingdomDuel.Domain.Enums",
    ];

    public static async Task GenerateApiClientAndExitAsync(this WebApplication app)
    {
        if (app.Configuration["generateclients"] != "true")
        {
            return;
        }

        var outputDir = Path.GetFullPath(
            Path.Combine(app.Environment.ContentRootPath, "..", "KingdomDuel.Client", "ApiClients"));
        Directory.CreateDirectory(outputDir);

        // Writes a single "<ClassName>.cs" and calls Environment.Exit when done.
        await app.GenerateClientsAndExitAsync(
            documentName: DocumentName,
            destinationPath: outputDir,
            csSettings: settings =>
            {
                settings.ClassName = "MatchesApiClient";
                settings.GenerateDtoTypes = false;
                settings.UseBaseUrl = false;
                settings.OperationNameGenerator = new SingleClientFromOperationIdOperationNameGenerator();
                settings.AdditionalNamespaceUsages = SharedNamespaceUsages;
                settings.CSharpGeneratorSettings.Namespace = "KingdomDuel.Client.ApiClients";
            },
            tsSettings: null);
    }
}
