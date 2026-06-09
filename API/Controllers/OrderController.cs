using System.Security.Claims;
using Application.DTOs;
using Application.DTOs.Order;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [Authorize]
        [HttpPost("create")]
        public async Task<BaseResponse<GetOrderDTO>> CreateOrder([FromBody] CreateOrderDTO createOrderDTO)
        {
            var buyerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return await _orderService.CreateOrderAsync(createOrderDTO, buyerId!);
        }

        [Authorize]
        [HttpGet("{orderId}")]
        public async Task<BaseResponse<GetOrderDTO>> GetOrderById(Guid orderId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return await _orderService.GetOrderByIdAsync(orderId, userId!);
        }

        [Authorize]
        [HttpGet("buyer")]
        public async Task<BaseResponse<IEnumerable<GetOrderDTO>>> GetBuyerOrders([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            var buyerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return await _orderService.GetBuyerOrdersAsync(buyerId!, pageNumber, pageSize);
        }

        [Authorize(Roles = "Vendor,Admin")]
        [HttpGet("shop/{shopId}")]
        public async Task<BaseResponse<IEnumerable<GetOrderDTO>>> GetShopOrders(Guid shopId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            var vendorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return await _orderService.GetShopOrdersAsync(shopId, vendorId!, pageNumber, pageSize);
        }

        [Authorize(Roles = "Vendor,Admin")]
        [HttpPut("status")]
        public async Task<BaseResponse<GetOrderDTO>> UpdateOrderStatus([FromBody] UpdateOrderStatusDTO updateStatusDTO)
        {
            var vendorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return await _orderService.UpdateOrderStatusAsync(updateStatusDTO, vendorId!);
        }

        [Authorize]
        [HttpPost("cancel")]
        public async Task<BaseResponse<string>> CancelOrder([FromBody] CancelOrderDTO cancelOrderDTO)
        {
            var buyerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return await _orderService.CancelOrderAsync(cancelOrderDTO, buyerId!);
        }

        [HttpGet("status")]
        public async Task<BaseResponse<IEnumerable<GetOrderDTO>>> GetOrdersByStatus([FromQuery] string status, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            return await _orderService.GetOrdersByStatusAsync(status, pageNumber, pageSize);
        }

        [Authorize]
        [HttpGet("stats")]
        public async Task<BaseResponse<Dictionary<string, int>>> GetOrderStats()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return await _orderService.GetOrderCountByStatusAsync(userId!);
        }

        [Authorize]
        [HttpPost("{orderId}/record-payment")]
        public async Task<BaseResponse<string>> RecordPayment(Guid orderId, [FromQuery] string paymentReference)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return await _orderService.RecordPaymentAsync(orderId, paymentReference, userId!);
        }
    }
}
