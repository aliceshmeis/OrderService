using OrderService.Domain.Entities;
using OrderService.Domain.DTOs;

namespace OrderService.Persistence.Repositories.Interfaces
{
    public interface IOrderRepository : IRepository<Order>
    {
        // Dapper methods for stored procedures
        Task<dynamic> GetOrderByIdSpAsync(int orderId);
        Task<dynamic> GetAllOrdersSpAsync();
        Task<dynamic> CreateOrderSpAsync(CreateOrderDto orderDto, int userId); // Changed to int
        Task<dynamic> UpdateOrderSpAsync(int orderId, UpdateOrderDto orderDto, int userId); // Changed to int
        Task<dynamic> DeleteOrderSpAsync(int orderId, int userId);
        Task<dynamic> GetOrdersByUserSpAsync(int userId);
        Task<dynamic> CancelOrderSpAsync(int orderId, int userId);

    }
}