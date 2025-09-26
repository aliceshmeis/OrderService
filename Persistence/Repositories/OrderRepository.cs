using OrderService.Domain.Models; // Add this line at the top with other usings
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using OrderService.Domain.DTOs;
using OrderService.Domain.Entities;
using OrderService.Persistence.Context;
using OrderService.Persistence.Repositories.Interfaces;
using System.Text.Json;

namespace OrderService.Persistence.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly string _connectionString;

        public OrderRepository(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        #region EF Core Methods (for direct entity access if needed)
        public async Task<IEnumerable<Order>> GetAllAsync()
        {
            return await _context.Orders
                .Where(x => !x.IsDeleted)
                .Include(x => x.OrderItems.Where(oi => !oi.IsDeleted))
                .ToListAsync();
        }

        public async Task<Order?> GetByIdAsync(int id)
        {
            return await _context.Orders
                .Include(x => x.OrderItems.Where(oi => !oi.IsDeleted))
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        }

        public async Task<Order> AddAsync(Order entity)
        {
            entity.CreatedDate = DateTime.UtcNow;
            _context.Orders.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<Order> UpdateAsync(Order entity)
        {
            entity.UpdatedDate = DateTime.UtcNow;
            _context.Orders.Update(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var order = await GetByIdAsync(id);
            if (order != null)
            {
                order.IsDeleted = true;
                order.UpdatedDate = DateTime.UtcNow;
                order.UpdatedBy = 1;

                foreach (var item in order.OrderItems)
                {
                    item.IsDeleted = true;
                    item.UpdatedDate = DateTime.UtcNow;
                    item.UpdatedBy = 1;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }
        #endregion

        #region Dapper Methods (Stored Procedures)
        public async Task<dynamic> GetOrderByIdSpAsync(int orderId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var result = await connection.QuerySingleOrDefaultAsync<string?>(
                "SELECT sp_get_order_by_id(@orderId)",
                new { orderId });

            if (string.IsNullOrEmpty(result))
                return new { errorCode = 404, data = (object?)null };

            return JsonSerializer.Deserialize<dynamic>(result) ?? new { errorCode = 500, data = (object?)null };
        }

        public async Task<dynamic> GetAllOrdersSpAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var result = await connection.QuerySingleOrDefaultAsync<string?>(
                "SELECT sp_get_all_orders()");

            if (string.IsNullOrEmpty(result))
                return new { errorCode = 0, data = new object[] { } };

            return JsonSerializer.Deserialize<dynamic>(result) ?? new { errorCode = 0, data = new object[] { } };
        }

        public async Task<dynamic> CreateOrderSpAsync(CreateOrderDto orderDto, int userId)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var orderItemsJson = JsonSerializer.Serialize(orderDto.OrderItems);

            // Debug logging
            Console.WriteLine($"=== DEBUG: Calling stored procedure ===");
            Console.WriteLine($"customerName: '{orderDto.CustomerName}'");
            Console.WriteLine($"customerEmail: '{orderDto.CustomerEmail}'");
            Console.WriteLine($"orderItems: '{orderItemsJson}'");
            Console.WriteLine($"userId: {userId}");
            Console.WriteLine("=====================================");

            try
            {
                var result = await connection.QuerySingleOrDefaultAsync<string?>(
                        "SELECT * FROM \"order\".sp_create_order(@customerName::VARCHAR, @customerEmail::VARCHAR, @orderItems::JSON, @userId::INTEGER)",
                    new
                    {
                        customerName = orderDto.CustomerName,
                        customerEmail = orderDto.CustomerEmail,
                        orderItems = orderItemsJson,
                        userId = userId
                    });

                Console.WriteLine($"Stored procedure returned: {result}");

                if (string.IsNullOrEmpty(result))
                    return new { errorCode = 500, data = (object?)null };

                var parsedResult = JsonSerializer.Deserialize<dynamic>(result);
                Console.WriteLine($"Parsed result: {parsedResult}");

                return parsedResult ?? new { errorCode = 500, data = (object?)null };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception calling stored procedure: {ex.Message}");
                Console.WriteLine($"Full exception: {ex}");
                throw;
            }
        }

        public async Task<dynamic> UpdateOrderSpAsync(int orderId, UpdateOrderDto orderDto, int userId)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            // Configure JSON serializer to use camelCase (itemId, quantity instead of ItemId, Quantity)
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var orderItemsJson = JsonSerializer.Serialize(orderDto.OrderItems, options);

            // Debug logging
            Console.WriteLine($"=== DEBUG: Update Order SP ===");
            Console.WriteLine($"orderId: {orderId}");
            Console.WriteLine($"customerName: '{orderDto.CustomerName}'");
            Console.WriteLine($"customerEmail: '{orderDto.CustomerEmail}'");
            Console.WriteLine($"status: '{orderDto.Status}'");
            Console.WriteLine($"orderItems: '{orderItemsJson}'");
            Console.WriteLine($"userId: {userId}");
            Console.WriteLine($"=============================");

            var result = await connection.QuerySingleOrDefaultAsync<string?>(
                "SELECT \"order\".sp_update_order(@orderId::INTEGER, @customerName::VARCHAR, @customerEmail::VARCHAR, @status::VARCHAR, @orderItems::JSON, @userId::INTEGER)",
                new
                {
                    orderId,
                    customerName = orderDto.CustomerName,
                    customerEmail = orderDto.CustomerEmail,
                    status = orderDto.Status,
                    orderItems = orderItemsJson,
                    userId = userId
                });

            Console.WriteLine($"Stored procedure returned: {result}");

            if (string.IsNullOrEmpty(result))
                return new { errorCode = 500, data = (object?)null };

            return JsonSerializer.Deserialize<dynamic>(result) ?? new { errorCode = 500, data = (object?)null };
        }
        public async Task<dynamic> DeleteOrderSpAsync(int orderId,int userId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var result = await connection.QuerySingleOrDefaultAsync<string?>(
                "SELECT sp_delete_order(@orderId, @updatedBy)",
                new { orderId, updatedBy = userId});

            if (string.IsNullOrEmpty(result))
                return new { errorCode = 500, data = (object?)null };

            return JsonSerializer.Deserialize<dynamic>(result) ?? new { errorCode = 500, data = (object?)null };
        }
        public async Task<dynamic> CancelOrderSpAsync(int orderId, int userId)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var result = await connection.QuerySingleOrDefaultAsync<string?>(
                "SELECT \"order\".sp_cancel_order(@orderId::INTEGER, @userId::INTEGER)",
                new { orderId, userId });

            if (string.IsNullOrEmpty(result))
                return new { errorCode = 500, data = (object?)null };

            return JsonSerializer.Deserialize<dynamic>(result) ?? new { errorCode = 500, data = (object?)null };
        }
        #endregion
        public async Task<dynamic> GetOrdersByUserSpAsync(int userId)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var sql = "SELECT \"order\".sp_get_orders_by_user(@userId)"; // quoted schema
            var result = await connection.QuerySingleOrDefaultAsync<string?>(sql, new { userId });

            if (string.IsNullOrEmpty(result))
                return new { errorCode = 0, data = Array.Empty<object>() };

            return JsonSerializer.Deserialize<dynamic>(result)
                   ?? new { errorCode = 0, data = Array.Empty<object>() };
        }
        // Add this method to your existing OrderRepository
        // Replace the entire CreateUserAsync method with this:
        public async Task<BaseResponse<object>> CreateUserAsync(string username, string email, string passwordHash)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);

                var result = await connection.QuerySingleAsync<dynamic>(
                    "SELECT * FROM \"order\".sp_create_user(@p_username, @p_email, @p_password_hash)",
                    new
                    {
                        p_username = username,
                        p_email = email,
                        p_password_hash = passwordHash
                    });

                int errorCode = (int)result.errorcode;

                if (errorCode == 0)
                    return BaseResponse<object>.Success("User created successfully");
                else if (errorCode == 409)
                    return BaseResponse<object>.Error("Username or email already exists", 409);
                else
                    return BaseResponse<object>.Error("Failed to create user", errorCode);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating user: {ex.Message}"); // Using Console since you don't have _logger
                return BaseResponse<object>.Error("Database error occurred", 500);
            }
        }

    }
}