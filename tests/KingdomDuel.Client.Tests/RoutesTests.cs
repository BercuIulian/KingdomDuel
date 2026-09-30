namespace KingdomDuel.Client.Tests;

public sealed class RoutesTests
{
    [Theory]
    [InlineData("login")]
    [InlineData("/login?returnUrl=%2Fplay")]
    [InlineData("LOGIN")]
    public void The_login_page_is_recognised_in_any_form(string path)
    {
        Routes.IsLogin(path).ShouldBeTrue();
    }

    [Fact]
    public void Other_pages_are_not_the_login()
    {
        Routes.IsLogin("game?code=ABC").ShouldBeFalse();
    }

    [Theory]
    [InlineData("", "/login")]
    [InlineData("/", "/login")]
    [InlineData("game?code=ABC", "/login?returnUrl=%2Fgame%3Fcode%3DABC")]
    public void The_login_link_remembers_where_to_come_back(string from, string link)
    {
        Routes.LoginWithReturnUrl(from).ShouldBe(link);
    }

    [Theory]
    [InlineData("/game?code=ABC", "/game?code=ABC")]
    [InlineData(null, Routes.AfterSignIn)]
    [InlineData("https://rau.example", Routes.AfterSignIn)]
    [InlineData("//rau.example", Routes.AfterSignIn)]
    [InlineData("/\\rau.example", Routes.AfterSignIn)]
    [InlineData("play", Routes.AfterSignIn)]
    public void Only_addresses_inside_the_site_are_followed_after_sign_in(string? returnUrl, string target)
    {
        Routes.SafeReturnUrl(returnUrl).ShouldBe(target);
    }

    [Fact]
    public void Match_codes_travel_escaped_in_the_address()
    {
        Routes.GameFor("A B").ShouldBe("/game?code=A%20B");
        Routes.LobbyFor("ABC123").ShouldBe("/lobby?code=ABC123");
    }

    [Fact]
    public void Profile_sections_hang_under_the_profile()
    {
        Routes.ProfileSection(null).ShouldBe("/profile");
        Routes.ProfileSection("history").ShouldBe("/profile/history");
    }
}
