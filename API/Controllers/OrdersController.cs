using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderService.Business.Services;
using OrderService.Domain.DTOs;
using OrderService.Domain.Models;
using System.Security.Claims;

namespace OrderService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
        {
            _orderService = orderService;
            _logger = logger;
        }

        /// <summary>
        /// Get all orders (Admin only)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")] // Only admins can see ALL orders
        [ProducesResponseType(typeof(BaseResponse<IEnumerable<OrderDto>>), 200)]
        [ProducesResponseType(typeof(BaseResponse), 500)]
        public async Task<ActionResult<BaseResponse<IEnumerable<OrderDto>>>> GetAllOrders()
        {
            _logger.LogInformation("GET /api/orders - Getting all orders (Admin only)");

            var result = await _orderService.GetAllOrdersAsync();

            if (result.ErrorCode != 0)
                return StatusCode(500, result);

            return Ok(result);
        }

        /// <summary>
        /// Get current user's orders (User and Admin)
        /// </summary>
        [HttpGet("my-orders")]
        [Authorize(Roles = "User,Admin")]
        [ProducesResponseType(typeof(BaseResponse<IEnumerable<OrderDto>>), 200)]
        [ProducesResponseType(typeof(BaseResponse), 500)]
        public async Task<ActionResult<BaseResponse<IEnumerable<OrderDto>>>> GetMyOrders()
        {
            _logger.LogInformation("GET /api/orders/my-orders - Getting user's orders");

            var userId = GetCurrentUserId();
            if (userId <= 0)
            {
                return Unauthorized(BaseResponse.Error("Invalid user authentication", 401));
            }

            var result = await _orderService.GetOrdersByUserAsync(userId);

            if (result.ErrorCode != 0)
                return StatusCode(500, result);

            return Ok(result);
        }

        /// <summary>
        /// Get order by ID (User can see own orders, Admin can see any)
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "User,Admin")]
        [ProducesResponseType(typeof(BaseResponse<OrderDto>), 200)]
        [ProducesResponseType(typeof(BaseResponse), 404)]
        [ProducesResponseType(typeof(BaseResponse), 403)]
        [ProducesResponseType(typeof(BaseResponse), 500)]
        public async Task<ActionResult<BaseResponse<OrderDto>>> GetOrderById(int id)
        {
            _logger.LogInformation("GET /api/orders/{OrderId} - Getting order by ID", id);

            var result = await _orderService.GetOrderByIdAsync(id);

            if (result.ErrorCode == 404)
                return NotFound(result);
            if (result.ErrorCode != 0)
                return StatusCode(500, result);

            // Check ownership for non-admin users
            var userRole = GetCurrentUserRole();
            var userId = GetCurrentUserId();

            // ADD THIS LOGGING HERE
            _logger.LogInformation("Debug - User ID: {UserId}, User Role: '{UserRole}', Order CreatedBy: {CreatedBy}",
                userId, userRole, result.Data?.CreatedBy);

            if (userRole != "Admin" && result.Data?.CreatedBy != userId)
            {
                _logger.LogWarning("Access denied - Role: '{Role}', UserID: {UserId}, OrderCreatedBy: {CreatedBy}",
                    userRole, userId, result.Data?.CreatedBy);
                return StatusCode(403, BaseResponse.Error("You can only view your own orders", 403));
            }

            return Ok(result);
        }

        /// <summary>
        /// Create a new order (User and Admin)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "User,Admin")]
        public async Task<ActionResult<BaseResponse<OrderDto>>> CreateOrder([FromBody] CreateOrderDto orderDto)
        {
            _logger.LogInformation("POST /api/orders - Creating new order for customer: {CustomerName}", orderDto?.CustomerName);

            if (!ModelState.IsValid)
            {
                var errors = string.Join(", ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                return BadRequest(BaseResponse.Error($"Validation failed: {errors}", 400));
            }

            // ADD THIS: Get the current user ID from JWT
            var userId = GetCurrentUserId();
            if (userId <= 0)
            {
                return Unauthorized(BaseResponse.Error("Invalid user authentication", 401));
            }

            // Pass the user ID to the business service
            var result = await _orderService.CreateOrderAsync(orderDto, userId);

            if (result.ErrorCode != 0)
                return StatusCode(500, result);

            return CreatedAtAction(nameof(GetOrderById), new { id = result.Data?.Id }, result);
        }

        /// <summary>
        /// Update an existing order (Admin only)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(BaseResponse<OrderDto>), 200)]
        [ProducesResponseType(typeof(BaseResponse), 400)]
        [ProducesResponseType(typeof(BaseResponse), 404)]
        [ProducesResponseType(typeof(BaseResponse), 500)]
        public async Task<ActionResult<BaseResponse<OrderDto>>> UpdateOrder(int id, [FromBody] UpdateOrderDto orderDto)
        {
            _logger.LogInformation("PUT /api/orders/{OrderId} - Updating order (Admin only)", id);

            if (!ModelState.IsValid)
            {
                var errors = string.Join(", ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                return BadRequest(BaseResponse.Error($"Validation failed: {errors}", 400));
            }
            var userId = GetCurrentUserId(); 

            var result = await _orderService.UpdateOrderAsync(id, orderDto, userId);

            if (result.ErrorCode == 404)
                return NotFound(result);
            if (result.ErrorCode != 0)
                return StatusCode(500, result);

            return Ok(result);
        }

        /// <summary>
        /// Delete an order (Admin only)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(BaseResponse), 200)]
        [ProducesResponseType(typeof(BaseResponse), 404)]
        [ProducesResponseType(typeof(BaseResponse), 500)]
        public async Task<ActionResult<BaseResponse>> DeleteOrder(int id)
        {
            _logger.LogInformation("DELETE /api/orders/{OrderId} - Deleting order (Admin only)", id);
            var userId = GetCurrentUserId();
            var result = await _orderService.DeleteOrderAsync(id , userId);

            if (result.ErrorCode == 404)
                return NotFound(result);
            if (result.ErrorCode != 0)
                return StatusCode(500, result);

            return Ok(result);
        }
        /// <summary>
        /// Cancel an order (User can cancel their own orders, Admin can cancel any)
        /// </summary>
        /// <param name="id">Order ID</param>
        /// <returns>Success or error message</returns>
        [HttpPatch("{id}/cancel")]
        [Authorize]
        [ProducesResponseType(typeof(BaseResponse<object>), 200)]
        [ProducesResponseType(typeof(BaseResponse), 400)]
        [ProducesResponseType(typeof(BaseResponse), 404)]
        public async Task<ActionResult<BaseResponse<object>>> CancelOrder(int id)
        {
            _logger.LogInformation("PATCH /api/orders/{OrderId}/cancel - Cancelling order", id);

            var userId = GetCurrentUserId();
            if (userId <= 0)
            {
                return Unauthorized(BaseResponse.Error("Invalid user authentication", 401));
            }

            var result = await _orderService.CancelOrderAsync(id, userId);

            if (result.ErrorCode == 404)
                return NotFound(result);
            if (result.ErrorCode == 403)
                return Forbid();
            if (result.ErrorCode != 0)
                return StatusCode(500, result);

            return Ok(result);
        }

        // Helper methods
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        private string GetCurrentUserRole()
        {
            // Debug what claims are actually available
            var allClaims = User.Claims.Select(c => $"{c.Type}={c.Value}").ToList();
            _logger.LogInformation("All JWT claims: {Claims}", string.Join(", ", allClaims));

            // Try different possible claim types
            var role = User.FindFirst("role")?.Value
                       ?? User.FindFirst("Role")?.Value
                       ?? User.FindFirst(ClaimTypes.Role)?.Value
                       ?? "User";

            _logger.LogInformation("Found role: '{Role}'", role);
            return role;
        }
    }
}