using Application.Abstractions;
using Application.Exceptions;
using Application.Modules.Identity.DTOs;
using Application.Modules.Identity.Services.Implementations;
using Application.Modules.Identity.Services.Interfaces;
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

    private AuthenticationService Sut() => new(_otp.Object, _users.Object, _refresh.Object, _tokens.Object,
        _sender.Object, _google.Object, _hasher.Object, Mock.Of<ILogger<AuthenticationService>>());

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
}
