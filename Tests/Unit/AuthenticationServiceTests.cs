using System;
using System.Threading;
using System.Threading.Tasks;
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

namespace FifeN.Tests.Unit
{
    /// <summary>
    /// Unit tests for the OTP verification path, isolating <see cref="AuthenticationService"/> from its
    /// infrastructure ports with Moq. The hasher is stubbed as an identity function so the stored
    /// <c>CodeHash</c> can be compared against a known plaintext code deterministically.
    /// </summary>
    public class AuthenticationServiceTests
    {
        private const string RawPhone = "08012345678";
        private const string NormalizedPhone = "+2348012345678";
        private const string Code = "123456";

        private readonly Mock<IPhoneVerificationStore> _otpStore = new(MockBehavior.Strict);
        private readonly Mock<IUserAccountStore> _userStore = new(MockBehavior.Strict);
        private readonly Mock<IRefreshTokenStore> _refreshStore = new(MockBehavior.Strict);
        private readonly Mock<ITokenService> _tokenService = new(MockBehavior.Strict);
        private readonly Mock<IOtpSender> _otpSender = new(MockBehavior.Strict);
        private readonly Mock<ISecureHasher> _hasher = new();
        private readonly Mock<IMfaStore> _mfaStore = new(MockBehavior.Strict);

        public AuthenticationServiceTests()
        {
            // Default: no account exists for the phone (a first-time buyer). Tests that need an existing
            // user override this after construction (later Moq setups win).
            _userStore.Setup(s => s.FindByPhoneAsync(NormalizedPhone, It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);
        }

        private AuthenticationService CreateSut()
        {
            // Identity hash: the "hash" of a code is the code itself, so comparisons are deterministic.
            _hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns<string>(s => s);
            return new AuthenticationService(
                _otpStore.Object, _userStore.Object, _refreshStore.Object, _tokenService.Object,
                _otpSender.Object, _hasher.Object, _mfaStore.Object, Mock.Of<ILogger<AuthenticationService>>());
        }

        private static PhoneVerification PendingChallenge() => new()
        {
            Id = Guid.NewGuid(),
            PhoneNumber = NormalizedPhone,
            CodeHash = Code,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(5),
            Status = OtpStatus.Pending,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        [Fact]
        public async Task VerifyOtpAsync_WithValidCodeForExistingUser_ReturnsSession()
        {
            var user = new User { Id = Guid.NewGuid(), FirstName = "Ada", LastName = "Obi", PhoneNumber = NormalizedPhone };
            var challenge = PendingChallenge();
            var expiry = DateTimeOffset.UtcNow.AddMinutes(30);

            _otpStore.Setup(s => s.GetLatestPendingAsync(NormalizedPhone, It.IsAny<CancellationToken>()))
                .ReturnsAsync(challenge);
            _otpStore.Setup(s => s.UpdateAsync(challenge, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _userStore.Setup(s => s.FindByPhoneAsync(NormalizedPhone, It.IsAny<CancellationToken>())).ReturnsAsync(user);
            _userStore.Setup(s => s.TouchLastActiveAsync(user.Id, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _userStore.Setup(s => s.GetRolesAsync(user, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { nameof(AppRole.User) });
            _refreshStore.Setup(s => s.IssueAsync(user.Id, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("refresh-token");
            _tokenService.Setup(s => s.CreateAccessToken(user, It.IsAny<System.Collections.Generic.IReadOnlyCollection<string>>(), false))
                .Returns(("access-token", expiry));

            var sut = CreateSut();
            var response = await sut.VerifyOtpAsync(new OtpVerifyRequest(RawPhone, Code), "127.0.0.1", CancellationToken.None);

            Assert.Equal("access-token", response.AccessToken);
            Assert.Equal("refresh-token", response.RefreshToken);
            Assert.Equal(user.Id, response.User.Id);
            Assert.Equal(OtpStatus.Verified, challenge.Status);
        }

        [Fact]
        public async Task VerifyOtpAsync_WithWrongCode_ThrowsUnauthorizedAndIncrementsAttempts()
        {
            var challenge = PendingChallenge();
            _otpStore.Setup(s => s.GetLatestPendingAsync(NormalizedPhone, It.IsAny<CancellationToken>()))
                .ReturnsAsync(challenge);
            _otpStore.Setup(s => s.UpdateAsync(challenge, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var sut = CreateSut();

            await Assert.ThrowsAsync<UnauthorizedException>(() =>
                sut.VerifyOtpAsync(new OtpVerifyRequest(RawPhone, "000000"), null, CancellationToken.None));
            Assert.Equal(1, challenge.Attempts);
            Assert.Equal(OtpStatus.Pending, challenge.Status);
        }

        [Fact]
        public async Task VerifyOtpAsync_WithThirdWrongCode_LocksTheChallenge()
        {
            var challenge = PendingChallenge();
            challenge.Attempts = 2; // two prior failures; this is the third.
            _otpStore.Setup(s => s.GetLatestPendingAsync(NormalizedPhone, It.IsAny<CancellationToken>()))
                .ReturnsAsync(challenge);
            _otpStore.Setup(s => s.UpdateAsync(challenge, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var sut = CreateSut();

            await Assert.ThrowsAsync<UnauthorizedException>(() =>
                sut.VerifyOtpAsync(new OtpVerifyRequest(RawPhone, "000000"), null, CancellationToken.None));
            Assert.Equal(OtpStatus.Locked, challenge.Status);
        }

        [Fact]
        public async Task VerifyOtpAsync_WhenChallengeExpired_ThrowsGone()
        {
            var challenge = PendingChallenge();
            challenge.ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1);
            _otpStore.Setup(s => s.GetLatestPendingAsync(NormalizedPhone, It.IsAny<CancellationToken>()))
                .ReturnsAsync(challenge);
            _otpStore.Setup(s => s.UpdateAsync(challenge, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var sut = CreateSut();

            await Assert.ThrowsAsync<GoneException>(() =>
                sut.VerifyOtpAsync(new OtpVerifyRequest(RawPhone, Code), null, CancellationToken.None));
            Assert.Equal(OtpStatus.Expired, challenge.Status);
        }

        [Fact]
        public async Task VerifyOtpAsync_WhenChallengeLocked_ThrowsLocked()
        {
            var challenge = PendingChallenge();
            challenge.Status = OtpStatus.Locked;
            _otpStore.Setup(s => s.GetLatestPendingAsync(NormalizedPhone, It.IsAny<CancellationToken>()))
                .ReturnsAsync(challenge);

            var sut = CreateSut();

            await Assert.ThrowsAsync<LockedException>(() =>
                sut.VerifyOtpAsync(new OtpVerifyRequest(RawPhone, Code), null, CancellationToken.None));
        }

        [Fact]
        public async Task VerifyOtpAsync_WithNoPendingChallenge_ThrowsUnauthorized()
        {
            _otpStore.Setup(s => s.GetLatestPendingAsync(NormalizedPhone, It.IsAny<CancellationToken>()))
                .ReturnsAsync((PhoneVerification?)null);

            var sut = CreateSut();

            await Assert.ThrowsAsync<UnauthorizedException>(() =>
                sut.VerifyOtpAsync(new OtpVerifyRequest(RawPhone, Code), null, CancellationToken.None));
        }

        [Fact]
        public async Task VerifyOtpAsync_ForMfaEnrolledAdminWithoutTotp_ThrowsMfaRequiredAndDoesNotConsumeOtp()
        {
            var admin = new User
            {
                Id = Guid.NewGuid(), FirstName = "Admin", LastName = "User",
                PhoneNumber = NormalizedPhone, IsAdmin = true, TwoFactorEnabled = true
            };
            _userStore.Setup(s => s.FindByPhoneAsync(NormalizedPhone, It.IsAny<CancellationToken>()))
                .ReturnsAsync(admin);

            var sut = CreateSut();

            // No TotpCode supplied → must fail before the OTP challenge is even read/consumed.
            await Assert.ThrowsAsync<MfaRequiredException>(() =>
                sut.VerifyOtpAsync(new OtpVerifyRequest(RawPhone, Code), null, CancellationToken.None));

            _otpStore.Verify(s => s.GetLatestPendingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _mfaStore.Verify(s => s.VerifyCodeAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task VerifyOtpAsync_ForMfaEnrolledAdminWithValidTotp_ReturnsMfaSatisfiedSession()
        {
            var admin = new User
            {
                Id = Guid.NewGuid(), FirstName = "Admin", LastName = "User",
                PhoneNumber = NormalizedPhone, IsAdmin = true, TwoFactorEnabled = true
            };
            var challenge = PendingChallenge();
            var expiry = DateTimeOffset.UtcNow.AddMinutes(30);

            _userStore.Setup(s => s.FindByPhoneAsync(NormalizedPhone, It.IsAny<CancellationToken>()))
                .ReturnsAsync(admin);
            _otpStore.Setup(s => s.GetLatestPendingAsync(NormalizedPhone, It.IsAny<CancellationToken>()))
                .ReturnsAsync(challenge);
            _otpStore.Setup(s => s.UpdateAsync(challenge, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mfaStore.Setup(s => s.VerifyCodeAsync(admin.Id, "654321", It.IsAny<CancellationToken>())).ReturnsAsync(true);
            _userStore.Setup(s => s.TouchLastActiveAsync(admin.Id, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _userStore.Setup(s => s.GetRolesAsync(admin, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { nameof(AppRole.Admin) });
            _refreshStore.Setup(s => s.IssueAsync(admin.Id, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("refresh-token");
            // amr=mfa must be granted (mfaSatisfied: true) only because the TOTP was verified this login.
            _tokenService.Setup(s => s.CreateAccessToken(admin, It.IsAny<System.Collections.Generic.IReadOnlyCollection<string>>(), true))
                .Returns(("access-token", expiry));

            var sut = CreateSut();
            var response = await sut.VerifyOtpAsync(
                new OtpVerifyRequest(RawPhone, Code, "654321"), "127.0.0.1", CancellationToken.None);

            Assert.Equal("access-token", response.AccessToken);
            Assert.Equal(OtpStatus.Verified, challenge.Status);
            _tokenService.VerifyAll();
        }

        [Fact]
        public async Task VerifyOtpAsync_ForMfaEnrolledAdminWithInvalidTotp_ThrowsUnauthorized()
        {
            var admin = new User
            {
                Id = Guid.NewGuid(), FirstName = "Admin", LastName = "User",
                PhoneNumber = NormalizedPhone, IsAdmin = true, TwoFactorEnabled = true
            };
            var challenge = PendingChallenge();

            _userStore.Setup(s => s.FindByPhoneAsync(NormalizedPhone, It.IsAny<CancellationToken>()))
                .ReturnsAsync(admin);
            _otpStore.Setup(s => s.GetLatestPendingAsync(NormalizedPhone, It.IsAny<CancellationToken>()))
                .ReturnsAsync(challenge);
            _otpStore.Setup(s => s.UpdateAsync(challenge, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mfaStore.Setup(s => s.VerifyCodeAsync(admin.Id, "000000", It.IsAny<CancellationToken>())).ReturnsAsync(false);

            var sut = CreateSut();

            await Assert.ThrowsAsync<UnauthorizedException>(() =>
                sut.VerifyOtpAsync(new OtpVerifyRequest(RawPhone, Code, "000000"), null, CancellationToken.None));
        }
    }
}
