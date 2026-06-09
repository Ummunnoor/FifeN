using Application.DTOs;
using Application.DTOs.Shop;
using Application.Services.Interfaces;
using Application.Services.Interfaces.Logging;

namespace Application.Services.Implementations

{
    /// <summary>
    /// Implementation of shop management service
    /// </summary>
    public class ShopService : IShopService
    {
        private readonly IGeneric<Domain.Entities.Product.Shop> _shopRepository;
        private readonly IAppLogger<ShopService> _logger;

        public ShopService(
            IGeneric<Domain.Entities.Product.Shop> shopRepository,
            IAppLogger<ShopService> logger)
        {
            _shopRepository = shopRepository;
            _logger = logger;
        }

        public async Task<BaseResponse<GetShopDTO>> GetShopByIdAsync(Guid shopId)
        {
            try
            {
                var shop = await _shopRepository.GetByIdAsync(shopId);
                if (shop == null)
                    return new BaseResponse<GetShopDTO>(false, "Shop not found");

                var shopDto = MapToGetShopDTO(shop);
                return new BaseResponse<GetShopDTO>(true, "Shop retrieved successfully", shopDto);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error getting shop by ID", ex);
                return new BaseResponse<GetShopDTO>(false, "An error occurred while retrieving shop");
            }
        }

        public async Task<BaseResponse<IEnumerable<GetShopDTO>>> GetAllShopsAsync(int pageNumber = 1, int pageSize = 20)
        {
            try
            {
                var shops = await _shopRepository.GetAllAsync();
                var activeShops = shops.Where(s => s.IsActive).ToList();

                var paginated = activeShops
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(MapToGetShopDTO)
                    .ToList();

                return new BaseResponse<IEnumerable<GetShopDTO>>(
                    true,
                    $"Retrieved {paginated.Count} shops",
                    paginated);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error getting all shops", ex);
                return new BaseResponse<IEnumerable<GetShopDTO>>(false, "An error occurred while retrieving shops", new List<GetShopDTO>());
            }
        }

        public async Task<BaseResponse<IEnumerable<GetShopDTO>>> GetShopsNearby(
            decimal latitude,
            decimal longitude,
            decimal radiusKm = 5,
            int pageNumber = 1,
            int pageSize = 20)
        {
            try
            {
                // TODO: Implement geospatial query using PostGIS
                // For now, return all shops and filter in memory
                var shops = await _shopRepository.GetAllAsync();
                
                var nearbyShops = shops
                    .Where(s => s.IsActive && s.Latitude.HasValue && s.Longitude.HasValue)
                    .Select(s => new
                    {
                        Shop = s,
                        Distance = CalculateDistance(latitude, longitude, (decimal)s.Latitude!, (decimal)s.Longitude!)
                    })
                    .Where(x => x.Distance <= radiusKm)
                    .OrderBy(x => x.Distance)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(x => {
                        var dto = MapToGetShopDTO(x.Shop);
                        dto.DistanceKm = decimal.Round(x.Distance, 2);
                        return dto;
                    })
                    .ToList();

                return new BaseResponse<IEnumerable<GetShopDTO>>(
                    true,
                    $"Found {nearbyShops.Count} nearby shops",
                    nearbyShops);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error getting nearby shops", ex);
                return new BaseResponse<IEnumerable<GetShopDTO>>(false, "An error occurred while searching nearby shops", new List<GetShopDTO>());
            }
        }

        public async Task<BaseResponse<IEnumerable<GetShopDTO>>> GetShopsByLocationAsync(
            string location,
            int pageNumber = 1,
            int pageSize = 20)
        {
            try
            {
                var shops = await _shopRepository.GetAllAsync();
                var filteredShops = shops
                    .Where(s => s.IsActive && (s.Address?.Contains(location, StringComparison.OrdinalIgnoreCase) ?? false))
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(MapToGetShopDTO)
                    .ToList();

                return new BaseResponse<IEnumerable<GetShopDTO>>(
                    true,
                    $"Found {filteredShops.Count} shops in {location}",
                    filteredShops);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error getting shops by location", ex);
                return new BaseResponse<IEnumerable<GetShopDTO>>(false, "An error occurred while searching shops", new List<GetShopDTO>());
            }
        }

        public async Task<BaseResponse<GetShopDTO>> CreateShopAsync(CreateShopDTO createShopDTO, string userId)
        {
            try
            {
                var newShop = new Domain.Entities.Product.Shop
                {
                    UserId = userId,
                    Name = createShopDTO.Name,
                    Description = createShopDTO.Description ?? "",
                    PhoneNumber = createShopDTO.PhoneNumber,
                    Address = createShopDTO.Address,
                    Latitude = createShopDTO.Latitude,
                    Longitude = createShopDTO.Longitude,
                    ImageUrl = createShopDTO.ImageUrl,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                await _shopRepository.AddAsync(newShop);
                _logger.LogInformation($"Shop created with ID: {newShop.Id} for user {userId}");

                return new BaseResponse<GetShopDTO>(
                    true,
                    "Shop created successfully",
                    MapToGetShopDTO(newShop));
            }
            catch (Exception ex)
            {
                _logger.LogError("Error creating shop", ex);
                return new BaseResponse<GetShopDTO>(false, "An error occurred while creating shop");
            }
        }

        public async Task<BaseResponse<GetShopDTO>> UpdateShopAsync(Guid shopId, UpdateShopDTO updateShopDTO, string userId)
        {
            try
            {
                var shop = await _shopRepository.GetByIdAsync(shopId);
                if (shop == null)
                    return new BaseResponse<GetShopDTO>(false, "Shop not found");

                if (shop.UserId != userId)
                    return new BaseResponse<GetShopDTO>(false, "Unauthorized to update this shop");

                shop.Name = updateShopDTO.Name ?? shop.Name;
                shop.Description = updateShopDTO.Description ?? shop.Description;
                shop.PhoneNumber = updateShopDTO.PhoneNumber ?? shop.PhoneNumber;
                shop.Address = updateShopDTO.Address ?? shop.Address;
                shop.Latitude = updateShopDTO.Latitude ?? shop.Latitude;
                shop.Longitude = updateShopDTO.Longitude ?? shop.Longitude;
                shop.ImageUrl = updateShopDTO.ImageUrl ?? shop.ImageUrl;
                shop.UpdatedAt = DateTime.UtcNow;

                await _shopRepository.UpdateAsync(shop);
                _logger.LogInformation($"Shop {shopId} updated");

                return new BaseResponse<GetShopDTO>(
                    true,
                    "Shop updated successfully",
                    MapToGetShopDTO(shop));
            }
            catch (Exception ex)
            {
                _logger.LogError("Error updating shop", ex);
                return new BaseResponse<GetShopDTO>(false, "An error occurred while updating shop");
            }
        }

        public async Task<BaseResponse<GetShopDTO>> GetUserShopAsync(string userId)
        {
            try
            {
                var shops = await _shopRepository.GetAllAsync();
                var userShop = shops.FirstOrDefault(s => s.UserId == userId && s.IsActive);

                if (userShop == null)
                    return new BaseResponse<GetShopDTO>(false, "No shop found for this user");

                return new BaseResponse<GetShopDTO>(
                    true,
                    "Shop retrieved successfully",
                    MapToGetShopDTO(userShop));
            }
            catch (Exception ex)
            {
                _logger.LogError("Error getting user shop", ex);
                return new BaseResponse<GetShopDTO>(false, "An error occurred while retrieving shop");
            }
        }

        public Task<BaseResponse<IEnumerable<object>>> GetShopProductsAsync(Guid shopId, int pageNumber = 1, int pageSize = 20)
        {
            // TODO: Implement with ProductService
            return Task.FromResult(new BaseResponse<IEnumerable<object>>(true, "Products retrieved", new List<object>()));
        }

        public Task<BaseResponse<IEnumerable<object>>> GetShopReviewsAsync(Guid shopId, int pageNumber = 1, int pageSize = 10)
        {
            // TODO: Implement with ReviewService
            return Task.FromResult(new BaseResponse<IEnumerable<object>>(true, "Reviews retrieved", new List<object>()));
        }

        public async Task<BaseResponse<string>> DeactivateShopAsync(Guid shopId, string userId)
        {
            try
            {
                var shop = await _shopRepository.GetByIdAsync(shopId);
                if (shop == null)
                    return new BaseResponse<string>(false, "Shop not found");

                if (shop.UserId != userId)
                    return new BaseResponse<string>(false, "Unauthorized to deactivate this shop");

                shop.IsActive = false;
                shop.UpdatedAt = DateTime.UtcNow;
                await _shopRepository.UpdateAsync(shop);

                return new BaseResponse<string>(true, "Shop deactivated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error deactivating shop", ex);
                return new BaseResponse<string>(false, "An error occurred while deactivating shop");
            }
        }

        public async Task<BaseResponse<string>> ActivateShopAsync(Guid shopId, string userId)
        {
            try
            {
                var shop = await _shopRepository.GetByIdAsync(shopId);
                if (shop == null)
                    return new BaseResponse<string>(false, "Shop not found");

                if (shop.UserId != userId)
                    return new BaseResponse<string>(false, "Unauthorized to activate this shop");

                shop.IsActive = true;
                shop.UpdatedAt = DateTime.UtcNow;
                await _shopRepository.UpdateAsync(shop);

                return new BaseResponse<string>(true, "Shop activated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error activating shop", ex);
                return new BaseResponse<string>(false, "An error occurred while activating shop");
            }
        }

        // Helper methods
        private GetShopDTO MapToGetShopDTO(Domain.Entities.Product.Shop shop)
        {
            return new GetShopDTO
            {
                Id = shop.Id,
                Name = shop.Name,
                Description = shop.Description,
                PhoneNumber = shop.PhoneNumber,
                Address = shop.Address,
                Latitude = shop.Latitude,
                Longitude = shop.Longitude,
                ImageUrl = shop.ImageUrl,
                IsVerified = shop.VerifiedAt.HasValue,
                AverageRating = shop.AverageRating,
                TotalReviews = shop.TotalReviews,
                ProductCount = shop.Products?.Count ?? 0,
                CreatedAt = shop.CreatedAt,
                IsActive = shop.IsActive
            };
        }

        // Haversine formula to calculate distance between two coordinates (in km)
        private decimal CalculateDistance(decimal lat1, decimal lon1, decimal lat2, decimal lon2)
        {
            const decimal earthRadiusKm = 6371;
            var dLat = (lat2 - lat1) * (decimal)Math.PI / 180;
            var dLon = (lon2 - lon1) * (decimal)Math.PI / 180;
            var a = (decimal)Math.Sin((double)dLat / 2) * (decimal)Math.Sin((double)dLat / 2) +
                    (decimal)Math.Cos((double)lat1 * Math.PI / 180) * (decimal)Math.Cos((double)lat2 * Math.PI / 180) *
                    (decimal)Math.Sin((double)dLon / 2) * (decimal)Math.Sin((double)dLon / 2);
            var c = 2 * (decimal)Math.Atan2(Math.Sqrt((double)a), Math.Sqrt((double)(1 - a)));
            return earthRadiusKm * c;
        }
    }
}
