using System.Security.Claims;


namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ShopController : ControllerBase
    {
        private readonly IShopService _shopService;

        public ShopController(IShopService shopService)
        {
            _shopService = shopService;
        }

        [HttpGet("{shopId}")]
        public async Task<BaseResponse<GetShopDTO>> GetShopById(Guid shopId)
        {
            return await _shopService.GetShopByIdAsync(shopId);
        }

        [HttpGet("all")]
        public async Task<BaseResponse<IEnumerable<GetShopDTO>>> GetAllShops([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            return await _shopService.GetAllShopsAsync(pageNumber, pageSize);
        }

        [HttpGet("nearby")]
        public async Task<BaseResponse<IEnumerable<GetShopDTO>>> GetShopsNearby(
            [FromQuery] decimal latitude,
            [FromQuery] decimal longitude,
            [FromQuery] decimal radiusKm = 5,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            return await _shopService.GetShopsNearby(latitude, longitude, radiusKm, pageNumber, pageSize);
        }

        [HttpGet("location")]
        public async Task<BaseResponse<IEnumerable<GetShopDTO>>> GetShopsByLocation([FromQuery] string location, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            return await _shopService.GetShopsByLocationAsync(location, pageNumber, pageSize);
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<BaseResponse<GetShopDTO>> GetUserShop()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return await _shopService.GetUserShopAsync(userId!);
        }

        [Authorize(Roles = "Vendor,Admin")]
        [HttpPost("create")]
        public async Task<BaseResponse<GetShopDTO>> CreateShop([FromBody] CreateShopDTO createShopDTO)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return await _shopService.CreateShopAsync(createShopDTO, userId!);
        }

        [Authorize(Roles = "Vendor,Admin")]
        [HttpPut("{shopId}")]
        public async Task<BaseResponse<GetShopDTO>> UpdateShop(Guid shopId, [FromBody] UpdateShopDTO updateShopDTO)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return await _shopService.UpdateShopAsync(shopId, updateShopDTO, userId!);
        }

        [HttpGet("{shopId}/products")]
        public async Task<BaseResponse<IEnumerable<object>>> GetShopProducts(Guid shopId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            return await _shopService.GetShopProductsAsync(shopId, pageNumber, pageSize);
        }

        [HttpGet("{shopId}/reviews")]
        public async Task<BaseResponse<IEnumerable<object>>> GetShopReviews(Guid shopId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            return await _shopService.GetShopReviewsAsync(shopId, pageNumber, pageSize);
        }

        [Authorize(Roles = "Vendor,Admin")]
        [HttpPost("{shopId}/deactivate")]
        public async Task<BaseResponse<string>> DeactivateShop(Guid shopId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return await _shopService.DeactivateShopAsync(shopId, userId!);
        }

        [Authorize(Roles = "Vendor,Admin")]
        [HttpPost("{shopId}/activate")]
        public async Task<BaseResponse<string>> ActivateShop(Guid shopId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return await _shopService.ActivateShopAsync(shopId, userId!);
        }
    }
}
