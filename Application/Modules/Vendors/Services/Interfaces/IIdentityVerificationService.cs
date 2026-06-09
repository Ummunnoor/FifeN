using System.Threading;
using System.Threading.Tasks;
using Domain.Entities.Enums;

namespace Application.Modules.Vendors.Services.Interfaces
{
    /// <summary>
    /// The outcome of a KYC check. Carries only what may be persisted (NDPA): never the raw identifier.
    /// </summary>
    public record KycResult(
        VerificationStatus Status,
        string? VerifiedName,
        bool NameMatch,
        decimal? NameMatchConfidence,
        string? ProviderReference);

    /// <summary>
    /// Port over a licensed KYC/identity provider (NIN for individuals, CAC for businesses). The raw
    /// identifier is passed in and forwarded to the provider; it is not returned and must not be stored.
    /// </summary>
    public interface IIdentityVerificationService
    {
        Task<KycResult> VerifyAsync(
            VerificationMethod method,
            string identifierToken,
            string expectedName,
            CancellationToken ct);
    }
}
