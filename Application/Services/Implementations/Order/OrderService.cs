using Application.DTOs;
using Application.DTOs.Order;
using Application.Services.Interfaces;
using Application.Services.Interfaces.Logging;
using Domain.Entities.Enums;
using Domain.Entities.Payment;


namespace Application.Services.Implementations.Order
{
    /// <summary>
    /// Implementation of order management service
    /// </summary>
    public class OrderService : IOrderService
    {
        private readonly IGeneric<Domain.Entities.Payment.Order> _orderRepository;
        private readonly IGeneric<OrderItem> _orderItemRepository;
        private readonly IGeneric<PaymentTransaction> _paymentRepository;
        private readonly IAppLogger<OrderService> _logger;

        public OrderService(
            IGeneric<Domain.Entities.Payment.Order> orderRepository,
            IGeneric<OrderItem> orderItemRepository,
            IGeneric<PaymentTransaction> paymentRepository,
            IAppLogger<OrderService> logger)
        {
            _orderRepository = orderRepository;
            _orderItemRepository = orderItemRepository;
            _paymentRepository = paymentRepository;
            _logger = logger;
        }

        public async Task<BaseResponse<GetOrderDTO>> CreateOrderAsync(CreateOrderDTO createOrderDTO, string buyerId)
        {
            try
            {
                // TODO: Implement full order creation logic
                // 1. Validate shop exists
                // 2. Validate products exist and quantities are available
                // 3. Create order
                // 4. Create order items
                // 5. Deduct inventory
                // 6. Send notification to vendor

                var newOrder = new Domain.Entities.Payment.Order
                {
                    ShopId = createOrderDTO.ShopId,
                    BuyerId = buyerId,
                    Status = OrderStatus.Pending,
                    TotalAmount = createOrderDTO.TotalAmount,
                    PaymentMethod = createOrderDTO.PaymentMethod,
                    BuyerNotes = createOrderDTO.BuyerNotes,
                    CreatedAt = DateTime.UtcNow
                };

                await _orderRepository.AddAsync(newOrder);
                _logger.LogInformation($"Order created: {newOrder.Id}");

                return new BaseResponse<GetOrderDTO>(
                    true,
                    "Order created successfully",
                    MapToGetOrderDTO(newOrder));
            }
            catch (Exception ex)
            {
                _logger.LogError("Error creating order", ex);
                return new BaseResponse<GetOrderDTO>(false, "An error occurred while creating order");
            }
        }

        public async Task<BaseResponse<GetOrderDTO>> GetOrderByIdAsync(Guid orderId, string userId)
        {
            try
            {
                var order = await _orderRepository.GetByIdAsync(orderId);
                if (order == null)
                    return new BaseResponse<GetOrderDTO>(false, "Order not found");

                // Verify user is buyer or seller
                if (order.BuyerId != userId && order.Shop?.UserId != userId)
                    return new BaseResponse<GetOrderDTO>(false, "Unauthorized to view this order");

                return new BaseResponse<GetOrderDTO>(
                    true,
                    "Order retrieved successfully",
                    MapToGetOrderDTO(order));
            }
            catch (Exception ex)
            {
                _logger.LogError("Error getting order", ex);
                return new BaseResponse<GetOrderDTO>(false, "An error occurred while retrieving order");
            }
        }

        public async Task<BaseResponse<IEnumerable<GetOrderDTO>>> GetBuyerOrdersAsync(
            string buyerId, int pageNumber = 1, int pageSize = 20)
        {
            try
            {
                var orders = await _orderRepository.GetAllAsync();
                var buyerOrders = orders
                    .Where(o => o.BuyerId == buyerId)
                    .OrderByDescending(o => o.CreatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(MapToGetOrderDTO)
                    .ToList();

                return new BaseResponse<IEnumerable<GetOrderDTO>>(
                    true,
                    $"Retrieved {buyerOrders.Count} orders",
                    buyerOrders);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error getting buyer orders", ex);
                return new BaseResponse<IEnumerable<GetOrderDTO>>(false, "An error occurred", new List<GetOrderDTO>());
            }
        }

        public async Task<BaseResponse<IEnumerable<GetOrderDTO>>> GetShopOrdersAsync(
            Guid shopId, string vendorId, int pageNumber = 1, int pageSize = 20)
        {
            try
            {
                var orders = await _orderRepository.GetAllAsync();
                var shopOrders = orders
                    .Where(o => o.ShopId == shopId)
                    .OrderByDescending(o => o.CreatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(MapToGetOrderDTO)
                    .ToList();

                return new BaseResponse<IEnumerable<GetOrderDTO>>(
                    true,
                    $"Retrieved {shopOrders.Count} orders",
                    shopOrders);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error getting shop orders", ex);
                return new BaseResponse<IEnumerable<GetOrderDTO>>(false, "An error occurred", new List<GetOrderDTO>());
            }
        }

        public async Task<BaseResponse<GetOrderDTO>> UpdateOrderStatusAsync(
            UpdateOrderStatusDTO updateStatusDTO, string vendorId)
        {
            try
            {
                var order = await _orderRepository.GetByIdAsync(updateStatusDTO.OrderId);
                if (order == null)
                    return new BaseResponse<GetOrderDTO>(false, "Order not found");

                // TODO: Verify vendorId owns the shop
                order.Status = updateStatusDTO.NewStatus;
                order.SellerNotes = updateStatusDTO.SellerNotes;
                order.UpdatedAt = DateTime.UtcNow;

                // Set status-specific timestamps
                if (updateStatusDTO.NewStatus == OrderStatus.Paid)
                    order.PaidAt = DateTime.UtcNow;
                else if (updateStatusDTO.NewStatus == OrderStatus.Shipped)
                    order.ShippedAt = DateTime.UtcNow;
                else if (updateStatusDTO.NewStatus == OrderStatus.Delivered)
                    order.DeliveredAt = DateTime.UtcNow;

                await _orderRepository.UpdateAsync(order);
                return new BaseResponse<GetOrderDTO>(true, "Order status updated", MapToGetOrderDTO(order));
            }
            catch (Exception ex)
            {
                _logger.LogError("Error updating order status", ex);
                return new BaseResponse<GetOrderDTO>(false, "An error occurred");
            }
        }

        public async Task<BaseResponse<string>> CancelOrderAsync(CancelOrderDTO cancelDTO, string buyerId)
        {
            try
            {
                var order = await _orderRepository.GetByIdAsync(cancelDTO.OrderId);
                if (order == null)
                    return new BaseResponse<string>(false, "Order not found");

                if (order.BuyerId != buyerId)
                    return new BaseResponse<string>(false, "Unauthorized");

                if (order.Status == OrderStatus.Shipped || order.Status == OrderStatus.Delivered)
                    return new BaseResponse<string>(false, "Cannot cancel order that has already shipped");

                order.Status = OrderStatus.Cancelled;
                order.CancelledAt = DateTime.UtcNow;
                await _orderRepository.UpdateAsync(order);

                return new BaseResponse<string>(true, "Order cancelled successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error cancelling order", ex);
                return new BaseResponse<string>(false, "An error occurred");
            }
        }

        public async Task<BaseResponse<IEnumerable<GetOrderDTO>>> GetOrdersByStatusAsync(
            string status, int pageNumber = 1, int pageSize = 20)
        {
            try
            {
                // TODO: Parse status string to OrderStatus enum
                var orders = await _orderRepository.GetAllAsync();
                var filteredOrders = orders
                    .OrderByDescending(o => o.CreatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(MapToGetOrderDTO)
                    .ToList();

                return new BaseResponse<IEnumerable<GetOrderDTO>>(true, "Orders retrieved", filteredOrders);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error getting orders by status", ex);
                return new BaseResponse<IEnumerable<GetOrderDTO>>(false, "An error occurred", new List<GetOrderDTO>());
            }
        }

        public Task<BaseResponse<Dictionary<string, int>>> GetOrderCountByStatusAsync(string userId)
        {
            try
            {
                // TODO: Implement dashboard stats
                var stats = new Dictionary<string, int>
                {
                    { "Pending", 0 },
                    { "Paid", 0 },
                    { "Shipped", 0 },
                    { "Delivered", 0 }
                };

                return Task.FromResult(new BaseResponse<Dictionary<string, int>>(true, "Stats retrieved", stats));
            }
            catch (Exception ex)
            {
                _logger.LogError("Error getting order stats", ex);
                return Task.FromResult(new BaseResponse<Dictionary<string, int>>(false, "An error occurred", new Dictionary<string, int>()));
            }
        }

        public Task<BaseResponse<string>> RecordPaymentAsync(Guid orderId, string paymentReference, string userId)
        {
            try
            {
                // TODO: Implement payment recording and order status update
                return Task.FromResult(new BaseResponse<string>(true, "Payment recorded successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError("Error recording payment", ex);
                return Task.FromResult(new BaseResponse<string>(false, "An error occurred"));
            }
        }

        // Helper
        private GetOrderDTO MapToGetOrderDTO(Domain.Entities.Payment.Order order)
        {
            return new GetOrderDTO
            {
                Id = order.Id,
                ShopId = order.ShopId,
                ShopName = order.Shop?.Name,
                Status = order.Status,
                TotalAmount = order.TotalAmount,
                PaymentMethod = order.PaymentMethod,
                Items = order.Items?.Select(oi => new OrderItemDTO
                {
                    ProductId = oi.ProductId,
                    ProductName = oi.ProductName,
                    UnitPrice = oi.UnitPrice,
                    Quantity = oi.Quantity,
                    LineTotal = oi.LineTotal,
                    ProductImageUrl = oi.ProductImageUrl
                }).ToList() ?? new List<OrderItemDTO>(),
                CreatedAt = order.CreatedAt,
                PaidAt = order.PaidAt,
                ShippedAt = order.ShippedAt,
                DeliveredAt = order.DeliveredAt,
                BuyerNotes = order.BuyerNotes,
                SellerNotes = order.SellerNotes
            };
        }
    }
}
