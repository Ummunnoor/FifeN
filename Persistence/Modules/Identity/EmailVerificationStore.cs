using Application.Modules.Identity.Services.Interfaces;
using Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Modules.Identity;

public sealed class EmailVerificationStore(FifeNDbContext db) : IEmailVerificationStore
{
    public Task<int> CountRequestsSinceAsync(string email, DateTimeOffset since, CancellationToken ct) =>
        db.EmailVerifications.CountAsync(v => v.Email == email && v.CreatedAtUtc >= since, ct);
    public async Task AddAsync(EmailVerification verification, CancellationToken ct)
    { db.EmailVerifications.Add(verification); await db.SaveChangesAsync(ct); }
    public Task<EmailVerification?> GetLatestPendingAsync(string email, CancellationToken ct) =>
        db.EmailVerifications.Where(v => v.Email == email && !v.IsConsumed)
            .OrderByDescending(v => v.CreatedAtUtc).FirstOrDefaultAsync(ct);
    public async Task UpdateAsync(EmailVerification verification, CancellationToken ct)
    { db.EmailVerifications.Update(verification); await db.SaveChangesAsync(ct); }
}
