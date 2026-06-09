using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Modules.Identity.Services.Interfaces;
using Domain.Entities.Enums;
using Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Modules.Identity
{
    /// <summary>EF Core store for transient OTP challenges.</summary>
    public sealed class PhoneVerificationStore(FifeNDbContext db) : IPhoneVerificationStore
    {
        public Task<int> CountRequestsSinceAsync(string phoneNumber, DateTimeOffset since, CancellationToken ct) =>
            db.PhoneVerifications.CountAsync(
                v => v.PhoneNumber == phoneNumber && v.CreatedAtUtc >= since, ct);

        public async Task AddAsync(PhoneVerification verification, CancellationToken ct)
        {
            db.PhoneVerifications.Add(verification);
            await db.SaveChangesAsync(ct);
        }

        public Task<PhoneVerification?> GetLatestPendingAsync(string phoneNumber, CancellationToken ct) =>
            db.PhoneVerifications
                .Where(v => v.PhoneNumber == phoneNumber && v.Status == OtpStatus.Pending)
                .OrderByDescending(v => v.CreatedAtUtc)
                .FirstOrDefaultAsync(ct);

        public Task UpdateAsync(PhoneVerification verification, CancellationToken ct)
        {
            db.PhoneVerifications.Update(verification);
            return db.SaveChangesAsync(ct);
        }
    }
}
