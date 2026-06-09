using Domain.Entities.Product;

namespace Domain.Interfaces.Vendor
{
    public interface IShopManagement
    {
        Task<Shop?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
        Task<bool> ExistsForUserIdAsync(string userId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Shop>> GetShopsByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    }
}