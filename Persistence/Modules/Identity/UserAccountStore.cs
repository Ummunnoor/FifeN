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
        public Task<User?> FindByEmailAsync(string email, CancellationToken ct) =>
            userManager.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == email.ToUpperInvariant(), ct);

        public async Task<User> CreateBuyerAsync(string email, string? firstName, string? lastName, string? googleSubjectId, CancellationToken ct)
        {
            var user = new User
            {
                Id = Guid.CreateVersion7(),
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                GoogleSubjectId = googleSubjectId,
                FirstName = firstName ?? string.Empty,
                LastName = lastName ?? string.Empty,
                Status = UserStatus.Active,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                LastActiveAtUtc = DateTimeOffset.UtcNow
            };

            var result = await userManager.CreateAsync(user);
            if (!result.Succeeded)
                throw new ConflictException(
                    string.Join("; ", result.Errors.Select(e => e.Description)));

            var roleResult = await userManager.AddToRoleAsync(user, nameof(AppRole.User));
            if (!roleResult.Succeeded)
                throw new ConflictException(string.Join("; ", roleResult.Errors.Select(e => e.Description)));
            return user;
        }

        public async Task<User> LinkGoogleAsync(User user, string subjectId, string? firstName, string? lastName, CancellationToken ct)
        {
            if (user.GoogleSubjectId is not null && user.GoogleSubjectId != subjectId)
                throw new ConflictException("A different Google account is already linked to this email.");
            if (user.GoogleSubjectId is null)
            {
                user.GoogleSubjectId = subjectId;
                if (string.IsNullOrWhiteSpace(user.FirstName)) user.FirstName = firstName ?? string.Empty;
                if (string.IsNullOrWhiteSpace(user.LastName)) user.LastName = lastName ?? string.Empty;
                var result = await userManager.UpdateAsync(user);
                if (!result.Succeeded) throw new ConflictException(string.Join("; ", result.Errors.Select(e => e.Description)));
            }
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

    }
}
