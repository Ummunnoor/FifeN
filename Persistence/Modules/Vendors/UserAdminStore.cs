using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Exceptions;
using Application.Modules.Vendors.Services.Interfaces;
using Domain.Entities.Enums;
using Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Modules.Vendors
{
    /// <summary>Account role/flag/status mutations backed by ASP.NET Identity.</summary>
    public sealed class UserAdminStore(UserManager<User> userManager, FifeNDbContext db) : IUserAdminStore
    {
        public async Task GrantVendorAsync(Guid userId, CancellationToken ct)
        {
            var user = await userManager.FindByIdAsync(userId.ToString())
                ?? throw new NotFoundException("User not found.");

            user.IsVendor = true;
            await userManager.UpdateAsync(user);

            if (!await userManager.IsInRoleAsync(user, nameof(AppRole.Vendor)))
                await userManager.AddToRoleAsync(user, nameof(AppRole.Vendor));
        }

        public Task SetStatusAsync(Guid userId, UserStatus status, CancellationToken ct) =>
            db.Users
                .Where(u => u.Id == userId)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.Status, status), ct);

        public async Task<IReadOnlyDictionary<Guid, UserStatus>> GetStatusesAsync(
            IReadOnlyCollection<Guid> userIds, CancellationToken ct)
        {
            if (userIds.Count == 0)
                return new Dictionary<Guid, UserStatus>();

            return await db.Users
                .AsNoTracking()
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.Status, ct);
        }

        public Task<bool> IsVendorAsync(Guid userId, CancellationToken ct) =>
            db.Users.AnyAsync(u => u.Id == userId && u.IsVendor, ct);

        public async Task<string?> GetDisplayNameAsync(Guid userId, CancellationToken ct)
        {
            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
            return user is null ? null : $"{user.FirstName} {user.LastName}".Trim();
        }
    }
}
