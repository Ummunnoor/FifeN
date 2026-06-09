using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Modules.Vendors.Services.Interfaces;
using Domain.Entities.Enums;
using Microsoft.Extensions.Logging;

namespace Persistence.Modules.Vendors
{
    /// <summary>
    /// Development KYC stub: auto-verifies and never persists or echoes the raw identifier. The
    /// production adapter for the licensed NIN/CAC provider will replace this behind
    /// <see cref="IIdentityVerificationService"/>.
    /// </summary>
    public sealed class DevIdentityVerificationService(ILogger<DevIdentityVerificationService> logger)
        : IIdentityVerificationService
    {
        public Task<KycResult> VerifyAsync(
            VerificationMethod method, string identifierToken, string expectedName, CancellationToken ct)
        {
            logger.LogInformation("[DEV KYC] {Method} verification requested for '{Name}' (identifier not logged).",
                method, expectedName);

            var result = new KycResult(
                Status: VerificationStatus.Verified,
                VerifiedName: expectedName,
                NameMatch: true,
                NameMatchConfidence: 1.0m,
                ProviderReference: $"DEV-{Guid.NewGuid():N}");

            return Task.FromResult(result);
        }
    }
}
