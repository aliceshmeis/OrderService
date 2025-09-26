using OrderService.Domain.DTOs;
using OrderService.Domain.Models;

namespace OrderService.Business.Services
{
    public interface IOrderService
    {
        Task<BaseResponse<IEnumerable<OrderDto>>> GetAllOrdersAsync();
        Task<BaseResponse<OrderDto>> GetOrderByIdAsync(int id);
        Task<BaseResponse<OrderDto>> CreateOrderAsync(CreateOrderDto orderDto, int userId);
        Task<BaseResponse<OrderDto>> UpdateOrderAsync(int id, UpdateOrderDto orderDto, int userId);
        Task<BaseResponse> DeleteOrderAsync(int id, int userId);
        Task<BaseResponse<IEnumerable<OrderDto>>> GetOrdersByUserAsync(int userId);
        Task<BaseResponse<object>> CancelOrderAsync(int orderId, int userId);
        
    }
}