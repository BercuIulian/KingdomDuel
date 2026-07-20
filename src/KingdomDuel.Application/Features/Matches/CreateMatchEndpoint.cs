using KingdomDuel.Domain.Entities.Matches;
using KingdomDuel.Shared.Requests.Matches;
using KingdomDuel.Shared.Responses.Matches;
using KingdomDuel.Shared.Validators.Matches;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using FastEndpoints;

namespace KingdomDuel.Application.Features.Matches;

internal sealed class CreateMatchEndpoint(AppDbContext dbContext)
    : Endpoint<CreateMatchRequest, CreateMatchResponse>
{
    public override void Configure()
    {
        Post("/matches");
        AllowAnonymous();
        Validator<CreateMatchValidator>();
        Description(b => b.WithName("CreateMatch"));
    }

    public override async Task HandleAsync(CreateMatchRequest req, CancellationToken ct)
    {
        string code;
        do
        {
            code = MatchCodeGenerator.Generate();
        } while (await dbContext.Matches.AnyAsync(m => m.Code == code, ct));

        var playerId = Guid.NewGuid().ToString("N");
        var match = new Match
        {
            Code = code,
            Player1 = new PlayerSlot { PlayerId = playerId, DisplayName = req.DisplayName },
        };

        dbContext.Matches.Add(match);
        await dbContext.SaveChangesAsync(ct);

        await Send.OkAsync(
            new CreateMatchResponse { MatchCode = match.Code, PlayerId = playerId, State = match.ToStateDto() },
            ct);
    }
}
