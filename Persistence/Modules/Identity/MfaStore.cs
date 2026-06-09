using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Application.Exceptions;
using Application.Modules.Identity.Services.Interfaces;
using Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;

namespace Persistence.Modules.Identity
{
    /// <summary>
    /// TOTP enrollment/verification on top of ASP.NET Identity's authenticator-key store
    /// (persisted in <c>AspNetUserTokens</c>). The shared secret is formatted into a standard
    /// <c>otpauth://totp</c> URI that Google Authenticator, Authy, etc. can scan.
    /// </summary>
    public sealed class MfaStore(UserManager<User> userManager) : IMfaStore
    {
        private const string Issuer = "TradeNaija";

        public async Task<string> ResetAndBuildAuthenticatorUriAsync(Guid userId, CancellationToken ct)
        {
            var user = await userManager.FindByIdAsync(userId.ToString())
                ?? throw new NotFoundException("User not found.");

            // A fresh key invalidates any prior enrollment, so re-enrolling is always safe.
            await userManager.ResetAuthenticatorKeyAsync(user);
            var key = await userManager.GetAuthenticatorKeyAsync(user)
                ?? throw new InvalidOperationException("Failed to generate an authenticator key.");

            var account = Uri.EscapeDataString(user.PhoneNumber ?? user.Id.ToString());
            return $"otpauth://totp/{Issuer}:{account}?secret={key}&issuer={Issuer}&digits=6&period=30";
        }

        public async Task<bool> VerifyAndEnableAsync(Guid userId, string code, CancellationToken ct)
        {
            var user = await userManager.FindByIdAsync(userId.ToString())
                ?? throw new NotFoundException("User not found.");

            if (!await VerifyTotpAsync(user, code))
                return false;

            await userManager.SetTwoFactorEnabledAsync(user, true);
            return true;
        }

        public async Task<bool> VerifyCodeAsync(Guid userId, string code, CancellationToken ct)
        {
            var user = await userManager.FindByIdAsync(userId.ToString())
                ?? throw new NotFoundException("User not found.");
            return await VerifyTotpAsync(user, code);
        }

        private Task<bool> VerifyTotpAsync(User user, string code) =>
            userManager.VerifyTwoFactorTokenAsync(
                user, userManager.Options.Tokens.AuthenticatorTokenProvider, code);
    }
}
