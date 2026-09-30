using KingdomDuel.Client.Authentication;

namespace KingdomDuel.Client.Tests;

public sealed class SentinelTests
{
    [Fact]
    public void Malformed_token_has_no_claims()
    {
        JwtPayload.ReadClaims("not-a-jwt").ShouldBeEmpty();
    }
}
