using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Exceptions;
using Application.Modules.Identity;
using Application.Modules.Vendors.DTOs;
using Application.Modules.Vendors.Services.Interfaces;
using Domain.Entities.Enums;
using Domain.Entities.Vendors;
using Microsoft.Extensions.Logging;

namespace Application.Modules.Vendors.Services.Implementations
{
    /// <summary>Vendor self-service: submit an application, read its status, update profile, public header.</summary>
    public class VendorService(
        IVendorRepository vendors,
        IUserAdminStore users,
        IIdentityVerificationService kyc,
        ILogger<VendorService> logger) : IVendorService
    {
        public async Task<VendorRequestResponse> SubmitRequestAsync(
            Guid userId, CreateVendorRequestRequest request, CancellationToken ct)
        {
            if (await users.IsVendorAsync(userId, ct))
                throw new ConflictException("You are already a vendor.");

            if (await vendors.HasPendingRequestAsync(userId, ct))
                throw new ConflictException("You already have a vendor application under review.");

            var businessName = request.BusinessName.Trim();
            if (await vendors.BusinessNameExistsAsync(businessName, ct))
                throw new ConflictException($"The business name '{businessName}' is already taken.");

            if (!NigerianPhoneNumber.TryNormalize(request.WhatsAppNumber, out var whatsApp))
                throw new InputValidationException(new Dictionary<string, string[]>
                {
                    [nameof(request.WhatsAppNumber)] = ["A valid Nigerian WhatsApp number is required."]
                });

            var kycResult = await kyc.VerifyAsync(request.Method, request.IdentifierToken, businessName, ct);
            if (!kycResult.NameMatch)
                throw new BusinessRuleException(
                    "Identity verification failed: the name on record does not match the business name.");

            var vendorRequest = new VendorRequest
            {
                Id = Guid.CreateVersion7(),
                UserId = userId,
                BusinessName = businessName,
                WhatsAppNumber = whatsApp,
                VerificationMethod = request.Method,
                VerificationStatus = kycResult.Status,
                NameMatch = kycResult.NameMatch,
                KycReference = kycResult.ProviderReference,
                Status = VendorRequestStatus.Pending,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };

            await vendors.AddRequestAsync(vendorRequest, ct);
            logger.LogInformation("Vendor request {RequestId} submitted by {UserId}.", vendorRequest.Id, userId);

            return ToResponse(vendorRequest);
        }

        public async Task<VendorRequestResponse> GetMyLatestRequestAsync(Guid userId, CancellationToken ct)
        {
            var request = await vendors.GetLatestRequestAsync(userId, ct)
                ?? throw new NotFoundException("You have not submitted a vendor application.");
            return ToResponse(request);
        }

        public async Task UpdateProfileAsync(Guid userId, UpdateVendorProfileRequest request, CancellationToken ct)
        {
            var profile = await vendors.GetProfileByUserAsync(userId, ct)
                ?? throw new NotFoundException("You do not have a vendor profile.");

            if (!NigerianPhoneNumber.TryNormalize(request.WhatsAppNumber, out var whatsApp))
                throw new InputValidationException(new Dictionary<string, string[]>
                {
                    [nameof(request.WhatsAppNumber)] = ["A valid Nigerian WhatsApp number is required."]
                });

            profile.WhatsAppNumber = whatsApp;
            await vendors.SaveProfileAsync(profile, ct);
        }

        public async Task<VendorPublicResponse> GetPublicAsync(Guid vendorProfileId, CancellationToken ct)
        {
            var profile = await vendors.GetProfileAsync(vendorProfileId, ct);
            if (profile is null || profile.User.Status != UserStatus.Active)
                throw new NotFoundException("Vendor not found.");

            var (average, count) = await vendors.GetRatingAsync(vendorProfileId, ct);
            return new VendorPublicResponse(
                profile.Id,
                profile.BusinessName,
                Verified: profile.VerificationStatus == VerificationStatus.Verified,
                AverageRating: average,
                ReviewCount: count);
        }

        private static VendorRequestResponse ToResponse(VendorRequest r) =>
            new(r.Id, r.Status, r.NameMatch, r.CreatedAtUtc);
    }
}
