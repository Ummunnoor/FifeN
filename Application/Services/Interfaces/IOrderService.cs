using Application.DTOs;
using Application.DTOs.Order;

namespace Application.Services.Interfaces
{
    /// <summary>
    /// Service for managing orders in the marketplace
    /// </summary>
    public interface IOrderService
    {
        /// <summary>
        /// Create a new order
        /// </summary>
        Task<BaseResponse<GetOrderDTO>> CreateOrderAsync(CreateOrderDTO createOrderDTO, string buyerId);

        /// <summary>
        /// Get order details by ID
        /// </summary>
        Task<BaseResponse<GetOrderDTO>> GetOrderByIdAsync(Guid orderId, string userId);

        /// <summary>
        /// Get all orders for a buyer (paginated)
        /// </summary>
        Task<BaseResponse<IEnumerable<GetOrderDTO>>> GetBuyerOrdersAsync(string buyerId, int pageNumber = 1, int pageSize = 20);

        /// <summary>
        /// Get all orders for a shop/vendor (paginated)
        /// </summary>
        Task<BaseResponse<IEnumerable<GetOrderDTO>>> GetShopOrdersAsync(Guid shopId, string vendorId, int pageNumber = 1, int pageSize = 20);

        /// <summary>
        /// Update order status (seller operation)
        /// </summary>
        Task<BaseResponse<GetOrderDTO>> UpdateOrderStatusAsync(UpdateOrderStatusDTO updateStatusDTO, string vendorId);

        /// <summary>
        /// Cancel an order (buyer operation, before shipped)
        /// </summary>
        Task<BaseResponse<string>> CancelOrderAsync(CancelOrderDTO cancelDTO, string buyerId);

        /// <summary>
        /// Get orders by status
        /// </summary>
        Task<BaseResponse<IEnumerable<GetOrderDTO>>> GetOrdersByStatusAsync(
            string status,
            int pageNumber = 1,
            int pageSize = 20);

        /// <summary>
        /// Get order count by status for dashboard
        /// </summary>
        Task<BaseResponse<Dictionary<string, int>>> GetOrderCountByStatusAsync(string userId);

        /// <summary>
        /// Record payment for an order
        /// </summary>
        Task<BaseResponse<string>> RecordPaymentAsync(Guid orderId, string paymentReference, string userId);
    }
}
