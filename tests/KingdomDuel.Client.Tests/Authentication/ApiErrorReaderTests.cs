using KingdomDuel.Client.ApiClients;
using KingdomDuel.Client.Authentication;

namespace KingdomDuel.Client.Tests.Authentication;

public sealed class ApiErrorReaderTests
{
    [Fact]
    public void Validation_messages_are_joined_into_one_line()
    {
        var body = """
            {"statusCode":400,"message":"One or more errors occurred!",
             "errors":{"userName":["Numele e luat."],"generalErrors":["Încă ceva.","Și asta."]}}
            """;

        ApiErrorReader.Describe(Error(body), "rezervă").ShouldBe("Numele e luat. Încă ceva. Și asta.");
    }

    [Fact]
    public void Without_validation_errors_the_message_is_used()
    {
        var body = """{"statusCode":429,"message":"Prea multe cereri."}""";

        ApiErrorReader.Describe(Error(body), "rezervă").ShouldBe("Prea multe cereri.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("<html>502</html>")]
    [InlineData("""{"errors":"nu-e-obiect"}""")]
    [InlineData("""{"message":42}""")]
    public void Anything_else_falls_back(string? body)
    {
        ApiErrorReader.Describe(Error(body), "rezervă").ShouldBe("rezervă");
    }

    private static ApiException Error(string? body) =>
        new("eroare", 400, body!, new Dictionary<string, IEnumerable<string>>(), null!);
}
