using System.Net;

namespace KingdomDuel.IntegrationTests;

public sealed class SentinelTests(ApiFactory factory)
{
    [Fact]
    public async Task Api_is_ready_against_a_fresh_database()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health/ready", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
