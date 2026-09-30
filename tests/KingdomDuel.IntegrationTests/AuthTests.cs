using System.Net;
using System.Net.Http.Json;
using KingdomDuel.Shared.Auth;
using KingdomDuel.Shared.Dtos.Auth;
using KingdomDuel.Shared.Dtos.Matches;
using KingdomDuel.Shared.Requests.Auth;
using KingdomDuel.Shared.Responses.Auth;

namespace KingdomDuel.IntegrationTests;

public sealed class AuthTests(ApiFactory api)
{
    [Fact]
    public async Task A_new_account_gets_a_token_that_opens_its_profile()
    {
        var player = await api.NewPlayer().RegisterAsync();

        var profile = await player.GetAsync<UserProfileDto>("/api/auth/profile");

        profile.UserName.ShouldBe(player.UserName);
    }

    [Fact]
    public async Task User_names_are_unique_whatever_the_case()
    {
        var first = await api.NewPlayer().RegisterAsync();

        var error = await api.NewPlayer().FailAsync(
            HttpMethod.Post,
            "/api/auth/register",
            new RegisterRequest { UserName = first.UserName.ToUpperInvariant(), Password = Player.Password });

        error.Messages.ShouldContain("Numele de utilizator este deja luat.");
    }

    [Fact]
    public async Task A_wrong_password_and_an_unknown_name_get_the_same_answer()
    {
        var player = await api.NewPlayer().RegisterAsync();

        var wrongPassword = await player.FailAsync(
            HttpMethod.Post,
            "/api/auth/login",
            new LoginRequest { UserName = player.UserName, Password = "parola-gresita" });

        var unknownName = await player.FailAsync(
            HttpMethod.Post,
            "/api/auth/login",
            new LoginRequest { UserName = "nimeni_" + Guid.NewGuid().ToString("N")[..8], Password = Player.Password });

        wrongPassword.Messages.ShouldContain("Nume de utilizator sau parolă greșite.");
        unknownName.Messages.ShouldContain("Nume de utilizator sau parolă greșite.");
    }

    [Fact]
    public async Task Login_ignores_the_case_of_the_name()
    {
        var player = await api.NewPlayer().RegisterAsync();

        var auth = await player.PostAsync<AuthResponse>(
            "/api/auth/login",
            new LoginRequest { UserName = player.UserName.ToUpperInvariant(), Password = Player.Password });

        auth.User.UserName.ShouldBe(player.UserName);
    }

    [Fact]
    public async Task Requests_without_a_token_are_turned_away()
    {
        var anonymous = api.NewPlayer();

        await anonymous.FailAsync(HttpMethod.Get, "/api/auth/profile", expected: HttpStatusCode.Unauthorized);
        await anonymous.FailAsync(HttpMethod.Post, "/api/matches", expected: HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logging_out_revokes_every_token_of_the_account()
    {
        var player = await api.NewPlayer().RegisterAsync();
        var firstToken = player.Token;

        var second = await player.PostAsync<AuthResponse>(
            "/api/auth/login",
            new LoginRequest { UserName = player.UserName, Password = Player.Password });

        player.UseToken(second.Token);
        using (await player.SendRawAsync(HttpMethod.Post, "/api/auth/logout"))
        {
        }

        await player.FailAsync(HttpMethod.Get, "/api/auth/profile", expected: HttpStatusCode.Unauthorized);

        player.UseToken(firstToken);
        await player.FailAsync(HttpMethod.Get, "/api/auth/profile", expected: HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Changing_the_password_signs_the_account_out_everywhere()
    {
        var player = await api.NewPlayer().RegisterAsync();

        var wrong = await player.FailAsync(
            HttpMethod.Put,
            "/api/auth/profile/password",
            new ChangePasswordRequest { CurrentPassword = "nu-e-parola", NewPassword = "parola-noua-1" });
        wrong.Messages.ShouldContain("Parola curentă este greșită.");

        using (var changed = await player.SendRawAsync(
            HttpMethod.Put,
            "/api/auth/profile/password",
            new ChangePasswordRequest { CurrentPassword = Player.Password, NewPassword = "parola-noua-1" }))
        {
            changed.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        }

        await player.FailAsync(HttpMethod.Get, "/api/auth/profile", expected: HttpStatusCode.Unauthorized);
        await player.FailAsync(
            HttpMethod.Post,
            "/api/auth/login",
            new LoginRequest { UserName = player.UserName, Password = Player.Password });
        await player.PostAsync<AuthResponse>(
            "/api/auth/login",
            new LoginRequest { UserName = player.UserName, Password = "parola-noua-1" });
    }

    [Fact]
    public async Task Renaming_the_account_asks_to_sign_in_again()
    {
        var player = await api.NewPlayer().RegisterAsync();

        var emailOnly = await player.PutAsync<ProfileUpdateResponse>(
            "/api/auth/profile",
            new UpdateProfileRequest { UserName = player.UserName, Email = "rege@cetate.ro" });
        emailOnly.RequiresSignIn.ShouldBeFalse();
        emailOnly.User.Email.ShouldBe("rege@cetate.ro");

        var renamed = await player.PutAsync<ProfileUpdateResponse>(
            "/api/auth/profile",
            new UpdateProfileRequest { UserName = player.UserName + "x" });
        renamed.RequiresSignIn.ShouldBeTrue();

        await player.FailAsync(HttpMethod.Get, "/api/auth/profile", expected: HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_name_already_taken_cannot_be_moved_into()
    {
        var taken = await api.NewPlayer().RegisterAsync();
        var player = await api.NewPlayer().RegisterAsync();

        var error = await player.FailAsync(
            HttpMethod.Put,
            "/api/auth/profile",
            new UpdateProfileRequest { UserName = taken.UserName });

        error.Messages.ShouldContain("Numele de utilizator este deja luat.");
    }

    [Fact]
    public async Task A_token_close_to_expiry_comes_back_renewed()
    {
        var player = await api.NewPlayer().RegisterAsync();

        using (var fresh = await player.SendRawAsync(HttpMethod.Get, "/api/auth/profile"))
        {
            fresh.Headers.Contains(AuthHeaders.RenewedToken).ShouldBeFalse();
        }

        api.Wait(TimeSpan.FromDays(6.5).TotalSeconds);

        string renewed;
        using (var old = await player.SendRawAsync(HttpMethod.Get, "/api/auth/profile"))
        {
            renewed = old.Headers.GetValues(AuthHeaders.RenewedToken).Single();
        }

        renewed.ShouldNotBe(player.Token);
        player.UseToken(renewed);
        await player.GetAsync<UserProfileDto>("/api/auth/profile");
    }

    [Fact]
    public async Task A_new_account_has_no_history()
    {
        var player = await api.NewPlayer().RegisterAsync();

        var history = await player.GetAsync<MatchHistoryDto>("/api/auth/profile/history");

        history.Played.ShouldBe(0);
        history.Entries.ShouldBeEmpty();
    }

    [Fact]
    public async Task Too_many_logins_from_one_address_are_slowed_down()
    {
        var player = api.NewPlayer();
        var login = new LoginRequest { UserName = "nimeni", Password = "parola-gresita" };

        for (var attempt = 0; attempt < 10; attempt++)
        {
            await player.FailAsync(HttpMethod.Post, "/api/auth/login", login);
        }

        using var response = await player.SendRawAsync(HttpMethod.Post, "/api/auth/login", login);

        response.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        response.Headers.RetryAfter.ShouldNotBeNull();
        var body = await response.Content.ReadFromJsonAsync<ApiError>(Player.Json, TestContext.Current.CancellationToken);
        body!.Message.ShouldStartWith("Prea multe încercări de autentificare");
    }
}
