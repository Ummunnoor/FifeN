using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Modules.Catalog.Services.Interfaces;
using Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Modules.Catalog
{
    /// <summary>Read access for categories.</summary>
    public sealed class CategoryRepository(FifeNDbContext db) : ICategoryRepository
    {
        public Task<Category?> GetAsync(Guid id, CancellationToken ct) =>
            db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.IsActive, ct);

        public async Task<IReadOnlyList<Category>> GetActiveAsync(CancellationToken ct) =>
            await db.Categories
                .AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
                .ToListAsync(ct);
    }
}
