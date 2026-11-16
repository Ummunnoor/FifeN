using Application.Abstractions;
using Application.Exceptions;
using Application.Modules.Identity.DTOs;
using Application.Modules.Identity.Services.Implementations;
using Application.Modules.Identity.Services.Interfaces;
using Domain.Entities.Enums;
using Domain.Entities.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FifeN.Tests.Unit;

public sealed class AuthenticationServiceTests
{
    private const string Email = "person@example.com";
    private const string Code = "123456";
    private readonly Mock<IEmailVerificationStore> _otp = new();
    private readonly Mock<IUserAccountStore> _users = new();
    private readonly Mock<IRefreshTokenStore> _refresh = new();
    private readonly Mock<ITokenService> _tokens = new();
    private readonly Mock<IEmailOtpSender> _sender = new();
    private readonly Mock<IGoogleIdTokenVerifier> _google = new();
    private readonly Mock<ISecureHasher> _hasher = new();
    private readonly Mock<IMfaStore> _mfa = new();

    private AuthenticationService Sut() => new(_otp.Object, _users.Object, _refresh.Object, _tokens.Object,
        _sender.Object, _google.Object, _hasher.Object, Mock.Of<ILogger<AuthenticationService>>(), _mfa.Object);

    private EmailVerification Challenge() => new()
    {
        Id = Guid.NewGuid(), Email = Email, CodeHash = Email + ":" + Code,
        ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(5), CreatedAtUtc = DateTimeOffset.UtcNow
    };

    [Fact]
    public async Task WrongCode_IncrementsAttemptsAndKeepsChallengeUntilLimit()
    {
        var challenge = Challenge();
        _hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns<string>(x => x);
        _otp.Setup(s => s.GetLatestPendingAsync(Email, It.IsAny<CancellationToken>())).ReturnsAsync(challenge);
        _otp.Setup(s => s.UpdateAsync(challenge, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            Sut().VerifyOtpAsync(new OtpVerifyRequest(Email, "000000"), null, CancellationToken.None));

        Assert.Equal(1, challenge.Attempts);
        Assert.False(challenge.IsConsumed);
    }

    [Fact]
    public async Task FifthWrongCode_ConsumesChallengeAndReturnsLocked()
    {
        var challenge = Challenge();
        challenge.Attempts = 4;
        _hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns<string>(x => x);
        _otp.Setup(s => s.GetLatestPendingAsync(Email, It.IsAny<CancellationToken>())).ReturnsAsync(challenge);
        _otp.Setup(s => s.UpdateAsync(challenge, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        await Assert.ThrowsAsync<LockedException>(() =>
            Sut().VerifyOtpAsync(new OtpVerifyRequest(Email, "000000"), null, CancellationToken.None));

        Assert.Equal(5, challenge.Attempts);
        Assert.True(challenge.IsConsumed);
    }

    [Fact]
    public async Task AdminWithoutAuthenticatorGetsSetupTokenButNoSessionTokens()
    {
        ArrangeValidOtp();
        var user = ActiveUser(isAdmin: true, twoFactorEnabled: false);
        _users.Setup(s => s.FindByEmailAsync(Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _users.Setup(s => s.GetRolesAsync(user, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { nameof(AppRole.Admin) });
        _tokens.Setup(s => s.CreateMfaSetupToken(user, It.IsAny<DateTimeOffset>())).Returns("setup-only-token");

        var response = await Sut().VerifyOtpAsync(
            new OtpVerifyRequest(Email, Code), null, CancellationToken.None);

        Assert.True(response.MfaEnrollmentRequired);
        Assert.Equal("setup-only-token", response.MfaSetupToken);
        Assert.Null(response.AccessToken);
        Assert.Null(response.RefreshToken);
        _refresh.Verify(s => s.IssueAsync(It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AdminWithEnrolledAuthenticatorReceivesChallengeInsteadOfSession()
    {
        ArrangeValidOtp();
        var user = ActiveUser(isAdmin: true, twoFactorEnabled: true);
        _users.Setup(s => s.FindByEmailAsync(Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _users.Setup(s => s.GetRolesAsync(user, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { nameof(AppRole.Admin) });
        _tokens.Setup(s => s.CreateMfaChallengeToken(user, It.IsAny<DateTimeOffset>())).Returns("mfa-challenge");

        var response = await Sut().VerifyOtpAsync(
            new OtpVerifyRequest(Email, Code), null, CancellationToken.None);

        Assert.True(response.MfaVerificationRequired);
        Assert.Equal("mfa-challenge", response.MfaChallengeToken);
        Assert.Null(response.AccessToken);
        Assert.Null(response.RefreshToken);
        _refresh.Verify(s => s.IssueAsync(It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AdminWithValidTotpAtChallengeStepReceivesSession()
    {
        var user = ActiveUser(isAdmin: true, twoFactorEnabled: true);
        _users.Setup(s => s.FindByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _users.Setup(s => s.GetRolesAsync(user, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { nameof(AppRole.Admin) });
        _mfa.Setup(s => s.VerifyCodeAsync(user.Id, "654321", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        ArrangeSession(user, nameof(AppRole.Admin));

        var response = await Sut().CompleteAdminMfaChallengeAsync(
            user.Id, "654321", null, CancellationToken.None);

        Assert.Equal("access-token", response.AccessToken);
        Assert.Equal("refresh-token", response.RefreshToken);
        _tokens.Verify(s => s.CreateAccessToken(user, It.IsAny<IReadOnlyCollection<string>>(), true,
            It.IsAny<DateTimeOffset>()), Times.Once);
    }

    [Fact]
    public async Task AdminWithInvalidTotpAtChallengeStepReceivesNoSession()
    {
        var user = ActiveUser(isAdmin: true, twoFactorEnabled: true);
        _users.Setup(s => s.FindByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _users.Setup(s => s.GetRolesAsync(user, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { nameof(AppRole.Admin) });
        _mfa.Setup(s => s.VerifyCodeAsync(user.Id, "000000", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var exception = await Assert.ThrowsAsync<UnauthorizedException>(() => Sut().CompleteAdminMfaChallengeAsync(
            user.Id, "000000", null, CancellationToken.None));

        Assert.Equal("Invalid authenticator code.", exception.Message);
        _refresh.Verify(s => s.IssueAsync(It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BuyerReceivesSessionWithoutTotp()
    {
        ArrangeValidOtp();
        var user = ActiveUser(isAdmin: false, twoFactorEnabled: false);
        _users.Setup(s => s.FindByEmailAsync(Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _users.Setup(s => s.GetRolesAsync(user, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { nameof(AppRole.User) });
        ArrangeSession(user, nameof(AppRole.User));

        var response = await Sut().VerifyOtpAsync(
            new OtpVerifyRequest(Email, Code), null, CancellationToken.None);

        Assert.Equal("access-token", response.AccessToken);
        Assert.False(response.MfaEnrollmentRequired);
        _mfa.Verify(s => s.VerifyCodeAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _tokens.Verify(s => s.CreateAccessToken(user, It.IsAny<IReadOnlyCollection<string>>(), false,
            It.IsAny<DateTimeOffset>()), Times.Once);
    }

    private void ArrangeValidOtp()
    {
        _hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns<string>(x => x);
        _otp.Setup(s => s.GetLatestPendingAsync(Email, It.IsAny<CancellationToken>())).ReturnsAsync(Challenge());
        _otp.Setup(s => s.UpdateAsync(It.IsAny<EmailVerification>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
    }

    private void ArrangeSession(User user, string role)
    {
        _users.Setup(s => s.GetRolesAsync(user, It.IsAny<CancellationToken>())).ReturnsAsync(new[] { role });
        _users.Setup(s => s.TouchLastActiveAsync(user.Id, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _refresh.Setup(s => s.IssueAsync(user.Id, It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(("refresh-token", DateTimeOffset.UtcNow));
        _tokens.Setup(s => s.CreateAccessToken(user, It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<bool>(), It.IsAny<DateTimeOffset>()))
            .Returns(("access-token", DateTimeOffset.UtcNow.AddMinutes(30)));
    }

    private static User ActiveUser(bool isAdmin, bool twoFactorEnabled) => new()
    {
        Id = Guid.NewGuid(), Email = Email, FirstName = "Test", LastName = "Account",
        IsAdmin = isAdmin, TwoFactorEnabled = twoFactorEnabled, Status = UserStatus.Active
    };
}
