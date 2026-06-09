using Domain.Entities.Product;
using Domain.Interfaces.Vendor;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories.Vendor
{
    public class ShopManagement : IShopManagement
    {
        private readonly FifeNDbContext _context;
        public ShopManagement(FifeNDbContext context)
        {
            _context = context;
        }
        public async Task<bool> ExistsForUserIdAsync(string userId, CancellationToken cancellationToken = default)
        {
            return await _context.Shops
            .AsNoTracking()
            .AnyAsync(s => s.UserId == userId, cancellationToken);
        }

        public async Task<Shop?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
        {
           return  await _context.Shops
           .AsNoTracking()
           .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);
        }

        public async Task<IReadOnlyList<Shop>> GetShopsByUserIdAsync(string userId, CancellationToken cancellationToken = default)
        {
            return await _context.Shops.AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
        }
    }
}