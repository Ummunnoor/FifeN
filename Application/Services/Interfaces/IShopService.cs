using Application.DTOs.Shop;
using Application.DTOs;

namespace Application.Services.Interfaces
{
    /// <summary>
    /// Service for managing vendor shops in the marketplace
    /// </summary>
    public interface IShopService
    {
        /// <summary>
        /// Get a shop by ID with complete details
        /// </summary>
        Task<BaseResponse<GetShopDTO>> GetShopByIdAsync(Guid shopId);

        /// <summary>
        /// Get all active shops (paginated)
        /// </summary>
        Task<BaseResponse<IEnumerable<GetShopDTO>>> GetAllShopsAsync(int pageNumber = 1, int pageSize = 20);

        /// <summary>
        /// Find shops near a user's location
        /// </summary>
        /// <param name="latitude">User's latitude</param>
        /// <param name="longitude">User's longitude</param>
        /// <param name="radiusKm">Search radius in kilometers</param>
        /// <param name="pageNumber">Page number</param>
        /// <param name="pageSize">Page size</param>
        Task<BaseResponse<IEnumerable<GetShopDTO>>> GetShopsNearby(
            decimal latitude,
            decimal longitude,
            decimal radiusKm = 5,
            int pageNumber = 1,
            int pageSize = 20);

        /// <summary>
        /// Get all shops in a specific area/city
        /// </summary>
        Task<BaseResponse<IEnumerable<GetShopDTO>>> GetShopsByLocationAsync(
            string location,
            int pageNumber = 1,
            int pageSize = 20);

        /// <summary>
        /// Create a new shop for an approved vendor
        /// </summary>
        Task<BaseResponse<GetShopDTO>> CreateShopAsync(CreateShopDTO createShopDTO, string userId);

        /// <summary>
        /// Update shop details (seller only)
        /// </summary>
        Task<BaseResponse<GetShopDTO>> UpdateShopAsync(Guid shopId, UpdateShopDTO updateShopDTO, string userId);

        /// <summary>
        /// Get shops owned by a specific user
        /// </summary>
        Task<BaseResponse<GetShopDTO>> GetUserShopAsync(string userId);

        /// <summary>
        /// Get shop's products
        /// </summary>
        Task<BaseResponse<IEnumerable<object>>> GetShopProductsAsync(Guid shopId, int pageNumber = 1, int pageSize = 20);

        /// <summary>
        /// Get shop reviews
        /// </summary>
        Task<BaseResponse<IEnumerable<object>>> GetShopReviewsAsync(Guid shopId, int pageNumber = 1, int pageSize = 10);

        /// <summary>
        /// Deactivate/close a shop
        /// </summary>
        Task<BaseResponse<string>> DeactivateShopAsync(Guid shopId, string userId);

        /// <summary>
        /// Activate a deactivated shop
        /// </summary>
        Task<BaseResponse<string>> ActivateShopAsync(Guid shopId, string userId);
    }
}
