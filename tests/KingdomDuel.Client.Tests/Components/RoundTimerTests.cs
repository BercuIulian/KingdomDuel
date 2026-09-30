using Bunit;
using KingdomDuel.Client.Components;

namespace KingdomDuel.Client.Tests.Components;

public sealed class RoundTimerTests : BunitContext
{
    [Fact]
    public void Nothing_is_shown_without_a_clock()
    {
        var timer = Render<RoundTimer>(p => p.Add(x => x.SecondsRemaining, null).Add(x => x.RoundSeconds, 45));

        timer.Markup.Trim().ShouldBeEmpty();
    }

    [Fact]
    public void The_bar_shows_the_share_of_the_round_left()
    {
        var timer = Render<RoundTimer>(p => p.Add(x => x.SecondsRemaining, 30).Add(x => x.RoundSeconds, 40));

        timer.Find(".fill").GetAttribute("style").ShouldBe("width: 75%");
        timer.Find(".round-timer").ClassList.ShouldNotContain("urgent");
    }

    [Fact]
    public void The_last_ten_seconds_are_urgent()
    {
        var timer = Render<RoundTimer>(p => p.Add(x => x.SecondsRemaining, 10).Add(x => x.RoundSeconds, 45));

        timer.Find(".round-timer").ClassList.ShouldContain("urgent");
    }

    [Fact]
    public void An_expired_round_says_it_waits_to_be_resolved()
    {
        var timer = Render<RoundTimer>(p => p.Add(x => x.SecondsRemaining, 0).Add(x => x.RoundSeconds, 45));

        timer.Find(".round-timer").ClassList.ShouldContain("expired");
        timer.Find(".note").TextContent.ShouldBe("Timpul a expirat, se așteaptă rezolvarea");
    }

    [Fact]
    public void The_clock_counts_down_on_its_own()
    {
        var timer = Render<RoundTimer>(p => p.Add(x => x.SecondsRemaining, 20).Add(x => x.RoundSeconds, 20));

        timer.WaitForAssertion(
            () => timer.Find(".track").GetAttribute("aria-valuenow").ShouldBe("20"),
            TimeSpan.FromSeconds(1));
        timer.WaitForAssertion(
            () => timer.Find(".fill").GetAttribute("style").ShouldNotBe("width: 100%"),
            TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void A_small_drift_from_the_server_is_ignored_and_a_large_one_resyncs()
    {
        var timer = Render<RoundTimer>(p => p.Add(x => x.SecondsRemaining, 30).Add(x => x.RoundSeconds, 40));

        timer.Render(p => p.Add(x => x.SecondsRemaining, 29).Add(x => x.RoundSeconds, 40));
        timer.Find(".track").GetAttribute("aria-valuenow").ShouldBe("30");

        timer.Render(p => p.Add(x => x.SecondsRemaining, 12).Add(x => x.RoundSeconds, 40));
        timer.Find(".track").GetAttribute("aria-valuenow").ShouldBe("12");
    }
}
