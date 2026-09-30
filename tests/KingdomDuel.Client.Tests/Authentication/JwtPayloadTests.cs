using KingdomDuel.Client.Authentication;

namespace KingdomDuel.Client.Tests.Authentication;

public sealed class JwtPayloadTests
{
    [Fact]
    public void Claims_are_read_from_the_payload_without_checking_the_signature()
    {
        var claims = JwtPayload.ReadClaims(Tokens.Fresh("regele.mihai"));

        claims.Single(c => c.Type == "name").Value.ShouldBe("regele.mihai");
        claims.Single(c => c.Type == "sub").Value.ShouldBe("11111111-1111-1111-1111-111111111111");
    }

    [Fact]
    public void Numbers_become_their_text()
    {
        var claims = JwtPayload.ReadClaims(Tokens.With(new { ver = 3 }));

        claims.Single().Value.ShouldBe("3");
    }

    [Theory]
    [InlineData("")]
    [InlineData("doar.doua")]
    [InlineData("a.!!!.c")]
    [InlineData("a.bnUtZS1qc29u.c")]
    public void A_malformed_token_has_no_claims_and_counts_as_expired(string token)
    {
        JwtPayload.ReadClaims(token).ShouldBeEmpty();
        JwtPayload.ReadExpiry(token).ShouldBeNull();
        JwtPayload.IsExpired(token).ShouldBeTrue();
    }

    [Fact]
    public void The_expiry_is_read_in_unix_seconds()
    {
        var expiry = JwtPayload.ReadExpiry(Tokens.With(new { exp = 1_900_000_000 }));

        expiry.ShouldBe(DateTimeOffset.FromUnixTimeSeconds(1_900_000_000));
    }

    [Fact]
    public void A_token_without_expiry_counts_as_expired()
    {
        JwtPayload.IsExpired(Tokens.With(new { name = "rege" })).ShouldBeTrue();
        JwtPayload.IsExpired(Tokens.With(new { exp = "mâine" })).ShouldBeTrue();
    }

    [Fact]
    public void A_token_is_expired_once_its_moment_passes()
    {
        JwtPayload.IsExpired(Tokens.Fresh()).ShouldBeFalse();
        JwtPayload.IsExpired(Tokens.Expired()).ShouldBeTrue();
    }
}
