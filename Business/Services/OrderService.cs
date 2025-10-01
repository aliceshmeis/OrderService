using Microsoft.Extensions.Logging;
using OrderService.Domain.DTOs;
using OrderService.Domain.Models;
using OrderService.Persistence.UnitOfWork;
using System.Text.Json;

namespace OrderService.Business.Services
{
    public class OrderBusinessService : IOrderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<OrderBusinessService> _logger;

        public OrderBusinessService(IUnitOfWork unitOfWork, ILogger<OrderBusinessService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<BaseResponse<IEnumerable<OrderDto>>> GetAllOrdersAsync()
        {
            try
            {
                _logger.LogInformation("Getting all orders");

                // Use stored procedure
                var result = await _unitOfWork.Orders.GetAllOrdersSpAsync();
                var jsonElement = (JsonElement)result;

                var errorCode = jsonElement.GetProperty("errorCode").GetInt32();
                if (errorCode != 0)
                {
                    _logger.LogWarning("Failed to get orders. Error code: {ErrorCode}", errorCode);
                    return BaseResponse<IEnumerable<OrderDto>>.Error("Failed to retrieve orders", errorCode);
                }

                // Parse the data from stored procedure result
                var dataProperty = jsonElement.GetProperty("data");
                var orders = JsonSerializer.Deserialize<IEnumerable<OrderDto>>(dataProperty.GetRawText(),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                _logger.LogInformation("Successfully retrieved {Count} orders", orders?.Count() ?? 0);
                return BaseResponse<IEnumerable<OrderDto>>.Success(orders ?? new List<OrderDto>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting all orders");
                return BaseResponse<IEnumerable<OrderDto>>.Error("An error occurred while retrieving orders", 500);
            }
        }

        public async Task<BaseResponse<OrderDto>> GetOrderByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation("Getting order with ID: {OrderId}", id);

                var result = await _unitOfWork.Orders.GetOrderByIdSpAsync(id);
                var jsonElement = (JsonElement)result;

                var errorCode = jsonElement.GetProperty("errorCode").GetInt32();
                if (errorCode != 0)
                {
                    _logger.LogWarning("Order not found. ID: {OrderId}, Error code: {ErrorCode}", id, errorCode);
                    return BaseResponse<OrderDto>.Error("Order not found", errorCode);
                }

                var dataProperty = jsonElement.GetProperty("data");
                var order = JsonSerializer.Deserialize<OrderDto>(dataProperty.GetRawText(),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                _logger.LogInformation("Successfully retrieved order with ID: {OrderId}", id);
                return BaseResponse<OrderDto>.Success(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting order with ID: {OrderId}", id);
                return BaseResponse<OrderDto>.Error("An error occurred while retrieving the order", 500);
            }
        }

        public async Task<BaseResponse<OrderDto>> CreateOrderAsync(CreateOrderDto orderDto, int userId)
        {
            try
            {
                _logger.LogInformation("Creating new order for customer: {CustomerName}", orderDto.CustomerName);

                var result = await _unitOfWork.Orders.CreateOrderSpAsync(orderDto, userId);
                var jsonElement = (JsonElement)result;

                var errorCode = jsonElement.GetProperty("errorCode").GetInt32();
                if (errorCode != 0)
                {
                    _logger.LogWarning("Failed to create order. Error code: {ErrorCode}", errorCode);
                    return BaseResponse<OrderDto>.Error("Failed to create order", errorCode);
                }

                // Get the created order data - only contains id and orderNumber
                var dataProperty = jsonElement.GetProperty("data");
                var orderId = dataProperty.GetProperty("id").GetInt32();

                // Retrieve the full order details using the ID
                var fullOrderResult = await _unitOfWork.Orders.GetOrderByIdSpAsync(orderId);
                var fullOrderElement = (JsonElement)fullOrderResult;

                if (fullOrderElement.GetProperty("errorCode").GetInt32() == 0)
                {
                    var fullOrderData = fullOrderElement.GetProperty("data");
                    var createdOrder = JsonSerializer.Deserialize<OrderDto>(fullOrderData.GetRawText(),
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    _logger.LogInformation("Successfully created order with ID: {OrderId}", orderId);
                    return BaseResponse<OrderDto>.Success(createdOrder, "Order created successfully");
                }

                // Fallback: return minimal order info if get fails
                _logger.LogInformation("Successfully created order with ID: {OrderId}", orderId);
                return BaseResponse<OrderDto>.Success(null, "Order created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating order for customer: {CustomerName}", orderDto.CustomerName);
                return BaseResponse<OrderDto>.Error("An error occurred while creating the order", 500);
            }
        }

        public async Task<BaseResponse<OrderDto>> UpdateOrderAsync(int id, UpdateOrderDto orderDto, int userId)
        {
            try
            {
                _logger.LogInformation("Updating order with ID: {OrderId}", id);

                var result = await _unitOfWork.Orders.UpdateOrderSpAsync(id, orderDto,  userId); 
                var jsonElement = (JsonElement)result;

                var errorCode = jsonElement.GetProperty("errorCode").GetInt32();
                if (errorCode != 0)
                {
                    _logger.LogWarning("Failed to update order. ID: {OrderId}, Error code: {ErrorCode}", id, errorCode);
                    return BaseResponse<OrderDto>.Error("Failed to update order", errorCode);
                }

                var dataProperty = jsonElement.GetProperty("data");
                var updatedOrder = JsonSerializer.Deserialize<OrderDto>(dataProperty.GetRawText(),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                _logger.LogInformation("Successfully updated order with ID: {OrderId}", id);
                return BaseResponse<OrderDto>.Success(updatedOrder, "Order updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating order with ID: {OrderId}", id);
                return BaseResponse<OrderDto>.Error("An error occurred while updating the order", 500);
            }
        }

        public async Task<BaseResponse> DeleteOrderAsync(int id, int userId)
        {
            try
            {
                _logger.LogInformation("Deleting order with ID: {OrderId}", id);

                var result = await _unitOfWork.Orders.DeleteOrderSpAsync(id,userId);
                var jsonElement = (JsonElement)result;

                var errorCode = jsonElement.GetProperty("errorCode").GetInt32();
                if (errorCode != 0)
                {
                    _logger.LogWarning("Failed to delete order. ID: {OrderId}, Error code: {ErrorCode}", id, errorCode);
                    return BaseResponse.Error("Failed to delete order", errorCode);
                }

                _logger.LogInformation("Successfully deleted order with ID: {OrderId}", id);
                return BaseResponse.Success("Order deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting order with ID: {OrderId}", id);
                return BaseResponse.Error("An error occurred while deleting the order", 500);
            }
        }
        public async Task<BaseResponse<IEnumerable<OrderDto>>> GetOrdersByUserAsync(int userId)
        {
            try
            {
                _logger.LogInformation("Getting orders for user: {UserId}", userId);

                // Use new stored procedure for user-specific orders
                var result = await _unitOfWork.Orders.GetOrdersByUserSpAsync(userId);
                var jsonElement = (JsonElement)result;

                var errorCode = jsonElement.GetProperty("errorCode").GetInt32();
                if (errorCode != 0)
                {
                    _logger.LogWarning("Failed to get orders for user: {UserId}. Error code: {ErrorCode}", userId, errorCode);
                    return BaseResponse<IEnumerable<OrderDto>>.Error("Failed to retrieve your orders", errorCode);
                }

                // Parse the data from stored procedure result
                var dataProperty = jsonElement.GetProperty("data");
                var orders = JsonSerializer.Deserialize<IEnumerable<OrderDto>>(dataProperty.GetRawText(),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                _logger.LogInformation("Successfully retrieved {Count} orders for user: {UserId}", orders?.Count() ?? 0, userId);
                return BaseResponse<IEnumerable<OrderDto>>.Success(orders ?? new List<OrderDto>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting orders for user: {UserId}", userId);
                return BaseResponse<IEnumerable<OrderDto>>.Error("An error occurred while retrieving your orders", 500);
            }
        }
        public async Task<BaseResponse<object>> CancelOrderAsync(int orderId, int userId)
        {
            try
            {
                _logger.LogInformation("Cancelling order with ID: {OrderId} by user: {UserId}", orderId, userId);

                var result = await _unitOfWork.Orders.CancelOrderSpAsync(orderId, userId);
                var jsonElement = (JsonElement)result;

                var errorCode = jsonElement.GetProperty("errorCode").GetInt32();
                if (errorCode != 0)
                {
                    _logger.LogWarning("Failed to cancel order. ID: {OrderId}, Error code: {ErrorCode}", orderId, errorCode);
                    return BaseResponse<object>.Error("Failed to cancel order", errorCode);
                }

                _logger.LogInformation("Successfully cancelled order with ID: {OrderId}", orderId);
                return BaseResponse<object>.Success("Order cancelled successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while cancelling order with ID: {OrderId}", orderId);
                return BaseResponse<object>.Error("An error occurred while cancelling the order", 500);
            }
        }
    }
}