using KingdomDuel.Domain.Entities.Users;
using KingdomDuel.Domain.Rounds;

namespace KingdomDuel.UnitTests.Domain;

public sealed class WindowTests
{
    private static readonly DateTimeOffset Until = DateTimeOffset.UnixEpoch.AddSeconds(10);

    [Fact]
    public void The_march_choice_counts_down_and_then_is_due()
    {
        var choice = new MarchChoice(1, 5, Until, null!);

        choice.SecondsLeftAt(DateTimeOffset.UnixEpoch.AddSeconds(4)).ShouldBe(6);
        choice.SecondsLeftAt(Until.AddSeconds(3)).ShouldBe(0);
        choice.IsDueAt(Until.AddSeconds(-0.1)).ShouldBeFalse();
        choice.IsDueAt(Until).ShouldBeTrue();
    }

    [Fact]
    public void The_bell_at_the_breach_counts_down_and_then_is_due()
    {
        var call = new BreachCall(2, 7, Until);

        call.SecondsLeftAt(DateTimeOffset.UnixEpoch.AddSeconds(4)).ShouldBe(6);
        call.SecondsLeftAt(Until.AddSeconds(3)).ShouldBe(0);
        call.IsDueAt(Until.AddSeconds(-0.1)).ShouldBeFalse();
        call.IsDueAt(Until).ShouldBeTrue();
    }

    [Fact]
    public void User_names_are_trimmed_and_compared_without_case()
    {
        var user = new User { UserName = "  Regele.Mihai ", PasswordHash = "x" };

        user.UserName.ShouldBe("Regele.Mihai");
        user.NormalizedUserName.ShouldBe("REGELE.MIHAI");
        User.Normalize(" regele.mihai").ShouldBe(user.NormalizedUserName);
    }
}
