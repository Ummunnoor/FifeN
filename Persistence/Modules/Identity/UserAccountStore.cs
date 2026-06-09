using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Exceptions;
using Application.Modules.Identity.Services.Interfaces;
using Domain.Entities.Enums;
using Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Modules.Identity
{
    /// <summary>Account lookup and creation backed by ASP.NET Identity's <see cref="UserManager{TUser}"/>.</summary>
    public sealed class UserAccountStore(UserManager<User> userManager, FifeNDbContext db) : IUserAccountStore
    {
        public Task<User?> FindByPhoneAsync(string phoneNumber, CancellationToken ct) =>
            userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber, ct);

        public async Task<User> CreateBuyerAsync(string phoneNumber, CancellationToken ct)
        {
            var user = new User
            {
                Id = Guid.CreateVersion7(),
                UserName = phoneNumber,
                PhoneNumber = phoneNumber,
                PhoneNumberConfirmed = true,
                FirstName = string.Empty,
                LastName = string.Empty,
                Status = UserStatus.Active,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                LastActiveAtUtc = DateTimeOffset.UtcNow
            };

            var result = await userManager.CreateAsync(user);
            if (!result.Succeeded)
                throw new ConflictException(
                    string.Join("; ", result.Errors.Select(e => e.Description)));

            await userManager.AddToRoleAsync(user, nameof(AppRole.User));
            return user;
        }

        public async Task<IReadOnlyCollection<string>> GetRolesAsync(User user, CancellationToken ct) =>
            (IReadOnlyCollection<string>)await userManager.GetRolesAsync(user);

        public async Task TouchLastActiveAsync(Guid userId, CancellationToken ct)
        {
            await db.Users
                .Where(u => u.Id == userId)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.LastActiveAtUtc, DateTimeOffset.UtcNow), ct);
        }

        public Task<User?> FindByIdAsync(Guid userId, CancellationToken ct) =>
            userManager.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);

        public Task<bool> PhoneInUseAsync(string phoneNumber, Guid excludingUserId, CancellationToken ct) =>
            userManager.Users.AnyAsync(
                u => u.Id != excludingUserId
                     && (u.PhoneNumber == phoneNumber || u.SecondaryPhoneNumber == phoneNumber),
                ct);

        public Task SetSecondaryPhoneAsync(Guid userId, string phoneNumber, CancellationToken ct) =>
            db.Users
                .Where(u => u.Id == userId)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.SecondaryPhoneNumber, phoneNumber), ct);
    }
}
