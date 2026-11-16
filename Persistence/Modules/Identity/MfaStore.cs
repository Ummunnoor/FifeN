using System;
using System.Data;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Application.Exceptions;
using Application.Modules.Identity.Services.Interfaces;
using Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Modules.Identity
{
    /// <summary>
    /// TOTP enrollment/verification on top of ASP.NET Identity's authenticator-key store
    /// (persisted in <c>AspNetUserTokens</c>). The shared secret is formatted into a standard
    /// <c>otpauth://totp</c> URI that Google Authenticator, Authy, etc. can scan.
    /// </summary>
    public sealed class MfaStore(UserManager<User> userManager, FifeNDbContext db) : IMfaStore
    {
        private const string Issuer = "Trade9ja";

        public async Task<string> ResetAndBuildAuthenticatorUriAsync(Guid userId, CancellationToken ct)
        {
            var strategy = db.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync<int, string>(0, async (_, _, executionCt) =>
            {
                await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, executionCt);

                // Avoid duplicate AspNetUserTokens inserts when a client retries or sends concurrent
                // enrollment requests for the same account. The transaction-scoped lock is cross-process.
                await db.Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT pg_advisory_xact_lock(hashtextextended({userId.ToString()}, 1))", executionCt);

                try
                {
                    executionCt.ThrowIfCancellationRequested();
                    var user = await userManager.FindByIdAsync(userId.ToString())
                        ?? throw new NotFoundException("User not found.");

                    if (user.TwoFactorEnabled)
                        throw new ConflictException("MFA is already enabled. Use the account recovery process to replace the authenticator.");

                    // Reuse an unconfirmed key so a retry returns the same authenticator URI.
                    var key = await userManager.GetAuthenticatorKeyAsync(user);
                    if (string.IsNullOrWhiteSpace(key))
                    {
                        executionCt.ThrowIfCancellationRequested();
                        var reset = await userManager.ResetAuthenticatorKeyAsync(user);
                        if (!reset.Succeeded)
                            throw new InvalidOperationException("Failed to generate an authenticator key.");
                        executionCt.ThrowIfCancellationRequested();
                        key = await userManager.GetAuthenticatorKeyAsync(user)
                            ?? throw new InvalidOperationException("Failed to generate an authenticator key.");
                    }

                    var account = Uri.EscapeDataString(user.PhoneNumber ?? user.Id.ToString());
                    var uri = $"otpauth://totp/{Issuer}:{account}?secret={key}&issuer={Issuer}&digits=6&period=30";
                    await transaction.CommitAsync(executionCt);
                    return uri;
                }
                catch
                {
                    try { await transaction.RollbackAsync(CancellationToken.None); }
                    finally { db.ChangeTracker.Clear(); }
                    throw;
                }
            }, verifySucceeded: null, cancellationToken: ct);
        }

        public async Task<bool> VerifyAndEnableAsync(Guid userId, string code, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            var user = await userManager.FindByIdAsync(userId.ToString())
                ?? throw new NotFoundException("User not found.");

            ct.ThrowIfCancellationRequested();
            if (!await VerifyTotpAsync(user, code))
                return false;

            var result = await userManager.SetTwoFactorEnabledAsync(user, true);
            ct.ThrowIfCancellationRequested();
            return result.Succeeded;
        }

        public async Task<bool> VerifyCodeAsync(Guid userId, string code, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            var user = await userManager.FindByIdAsync(userId.ToString())
                ?? throw new NotFoundException("User not found.");
            ct.ThrowIfCancellationRequested();
            return await VerifyTotpAsync(user, code);
        }

        private Task<bool> VerifyTotpAsync(User user, string code) =>
            userManager.VerifyTwoFactorTokenAsync(
                user, userManager.Options.Tokens.AuthenticatorTokenProvider, code);
    }
}
