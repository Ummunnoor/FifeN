using Application.Modules.Identity.Services.Interfaces;
using Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Persistence.Modules.Identity;

public sealed class EmailVerificationStore(FifeNDbContext db) : IEmailVerificationStore
{
    public async Task<bool> TryAddWithinLimitAsync(EmailVerification verification, DateTimeOffset since, int limit, CancellationToken ct)
    {
        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
            try
            {
                // Serialize requests for this email while counting and inserting. The value is parameterized by EF.
                await db.Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT pg_advisory_xact_lock(hashtextextended({verification.Email}, 0))", ct);

                // Handles an ambiguous commit if the execution strategy retries after PostgreSQL committed.
                if (await db.EmailVerifications.AnyAsync(v => v.Id == verification.Id, ct))
                {
                    await transaction.CommitAsync(ct);
                    return true;
                }

                var count = await db.EmailVerifications.CountAsync(
                    v => v.Email == verification.Email && v.CreatedAtUtc >= since, ct);
                if (count >= limit)
                {
                    await transaction.CommitAsync(ct);
                    return false;
                }

                db.EmailVerifications.Add(verification);
                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                return true;
            }
            catch
            {
                // SaveChanges may have accepted the entity before a transient commit failure. Detach it
                // so a retry can safely query for an already-committed row or add it again after rollback.
                db.Entry(verification).State = EntityState.Detached;
                throw;
            }
        });
    }
    public Task<EmailVerification?> GetLatestPendingAsync(string email, CancellationToken ct) =>
        db.EmailVerifications.Where(v => v.Email == email && !v.IsConsumed)
            .OrderByDescending(v => v.CreatedAtUtc).FirstOrDefaultAsync(ct);
    public async Task UpdateAsync(EmailVerification verification, CancellationToken ct)
    { db.EmailVerifications.Update(verification); await db.SaveChangesAsync(ct); }
}
