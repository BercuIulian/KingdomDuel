using FluentValidation.TestHelper;
using KingdomDuel.Shared.Requests.Auth;
using KingdomDuel.Shared.Validators.Auth;

namespace KingdomDuel.UnitTests.Shared;

public sealed class AuthValidatorTests
{
    private readonly RegisterValidator register = new();
    private readonly LoginValidator login = new();
    private readonly ChangePasswordValidator changePassword = new();
    private readonly UpdateProfileValidator updateProfile = new();

    [Theory]
    [InlineData("abc")]
    [InlineData("regele.mihai_2-a")]
    [InlineData("abcdefghijklmnopqrstuvwx")]
    public void Valid_user_names_pass(string userName)
    {
        register.TestValidate(Register(userName)).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("ab")]
    [InlineData("abcdefghijklmnopqrstuvwxy")]
    [InlineData("ștefan")]
    [InlineData("rege mare")]
    [InlineData("rege@cetate")]
    public void Invalid_user_names_are_refused(string userName)
    {
        register.TestValidate(Register(userName)).ShouldHaveValidationErrorFor(r => r.UserName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1234567")]
    public void Short_passwords_are_refused(string password)
    {
        register.TestValidate(Register(password: password)).ShouldHaveValidationErrorFor(r => r.Password);
    }

    [Fact]
    public void Passwords_longer_than_128_characters_are_refused()
    {
        register.TestValidate(Register(password: new string('a', 129))).ShouldHaveValidationErrorFor(r => r.Password);
        register.TestValidate(Register(password: new string('a', 128))).ShouldNotHaveValidationErrorFor(r => r.Password);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("rege@cetate.ro")]
    public void Email_is_optional_but_must_be_valid_when_given(string? email)
    {
        register.TestValidate(Register(email: email)).ShouldNotHaveValidationErrorFor(r => r.Email);
    }

    [Fact]
    public void A_malformed_email_is_refused()
    {
        register.TestValidate(Register(email: "nu-e-email")).ShouldHaveValidationErrorFor(r => r.Email);
        updateProfile.TestValidate(new UpdateProfileRequest { UserName = "rege", Email = "nu-e-email" })
            .ShouldHaveValidationErrorFor(r => r.Email);
    }

    [Fact]
    public void Login_needs_both_fields_but_does_not_check_their_shape()
    {
        login.TestValidate(new LoginRequest { UserName = "x", Password = "y" }).ShouldNotHaveAnyValidationErrors();

        var empty = login.TestValidate(new LoginRequest());
        empty.ShouldHaveValidationErrorFor(r => r.UserName);
        empty.ShouldHaveValidationErrorFor(r => r.Password);
    }

    [Fact]
    public void The_new_password_must_differ_from_the_current_one()
    {
        changePassword.TestValidate(new ChangePasswordRequest { CurrentPassword = "parola-veche", NewPassword = "parola-veche" })
            .ShouldHaveValidationErrorFor(r => r.NewPassword);

        changePassword.TestValidate(new ChangePasswordRequest { CurrentPassword = "parola-veche", NewPassword = "parola-noua" })
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Changing_the_password_needs_the_current_one()
    {
        changePassword.TestValidate(new ChangePasswordRequest { NewPassword = "parola-noua" })
            .ShouldHaveValidationErrorFor(r => r.CurrentPassword);
    }

    [Fact]
    public void The_profile_follows_the_same_user_name_rules()
    {
        updateProfile.TestValidate(new UpdateProfileRequest { UserName = "ab" }).ShouldHaveValidationErrorFor(r => r.UserName);
        updateProfile.TestValidate(new UpdateProfileRequest { UserName = "rege" }).ShouldNotHaveAnyValidationErrors();
    }

    private static RegisterRequest Register(string userName = "rege", string password = "parola-buna", string? email = null) =>
        new() { UserName = userName, Password = password, Email = email };
}
