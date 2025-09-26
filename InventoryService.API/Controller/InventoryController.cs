using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InventoryService.Business.Services;
using InventoryService.Domain.DTOs;
using InventoryService.Domain.Models;
using System.Security.Claims;

namespace InventoryService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class InventoryController : ControllerBase
    {
        private readonly IInventoryService _inventoryService;
        private readonly ILogger<InventoryController> _logger;

        public InventoryController(IInventoryService inventoryService, ILogger<InventoryController> logger)
        {
            _inventoryService = inventoryService;
            _logger = logger;
        }

        /// <summary>
        /// Get all inventory items (Users and Admins can view)
        /// </summary>
        /// <returns>List of inventory items</returns>
        [HttpGet]
        [Authorize(Roles = "User,Admin")]
        [ProducesResponseType(typeof(BaseResponse<IEnumerable<InventoryItemDto>>), 200)]
        [ProducesResponseType(typeof(BaseResponse), 500)]
        public async Task<ActionResult<BaseResponse<IEnumerable<InventoryItemDto>>>> GetAllItems()
        {
            _logger.LogInformation("GET /api/inventory - Getting all inventory items");

            var result = await _inventoryService.GetAllItemsAsync();

            if (result.ErrorCode != 0)
                return StatusCode(500, result);

            return Ok(result);
        }

        /// <summary>
        /// Get inventory item by ID (Users and Admins can view details)
        /// </summary>
        /// <param name="id">Item ID</param>
        /// <returns>Inventory item details</returns>
        [HttpGet("{id}")]
        [Authorize(Roles = "User,Admin")]
        [ProducesResponseType(typeof(BaseResponse<InventoryItemDto>), 200)]
        [ProducesResponseType(typeof(BaseResponse), 404)]
        [ProducesResponseType(typeof(BaseResponse), 500)]
        public async Task<ActionResult<BaseResponse<InventoryItemDto>>> GetItemById(int id)
        {
            _logger.LogInformation("GET /api/inventory/{ItemId} - Getting item by ID", id);

            var result = await _inventoryService.GetItemByIdAsync(id);

            if (result.ErrorCode == 404)
                return NotFound(result);
            if (result.ErrorCode != 0)
                return StatusCode(500, result);

            return Ok(result);
        }

        /// <summary>
        /// Create a new inventory item (Admin only)
        /// </summary>
        /// <param name="createItemDto">Item creation data</param>
        /// <returns>Created inventory item</returns>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(BaseResponse<InventoryItemDto>), 201)]
        [ProducesResponseType(typeof(BaseResponse), 400)]
        [ProducesResponseType(typeof(BaseResponse), 409)]
        [ProducesResponseType(typeof(BaseResponse), 500)]
        public async Task<ActionResult<BaseResponse<InventoryItemDto>>> CreateItem([FromBody] CreateInventoryItemDto createItemDto)
        {
            _logger.LogInformation("POST /api/inventory - Creating new inventory item: {ItemName} (Admin only)", createItemDto?.ItemName);

            if (!ModelState.IsValid)
            {
                var errors = string.Join(", ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                return BadRequest(BaseResponse.Error($"Validation failed: {errors}", 400));
            }

            var userId = GetCurrentUserId();
            if (userId <= 0)
            {
                _logger.LogWarning("Invalid user ID from JWT token");
                return Unauthorized(BaseResponse.Error("Invalid user authentication", 401));
            }

            var result = await _inventoryService.CreateItemAsync(createItemDto, userId);

            if (result.ErrorCode == 409)
                return Conflict(result);
            if (result.ErrorCode == 400)
                return BadRequest(result);
            if (result.ErrorCode != 0)
                return StatusCode(500, result);

            return CreatedAtAction(nameof(GetItemById), new { id = result.Data?.Id }, result);
        }

        /// <summary>
        /// Update an existing inventory item (Admin only)
        /// </summary>
        /// <param name="id">Item ID</param>
        /// <param name="updateItemDto">Item update data</param>
        /// <returns>Updated inventory item</returns>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(BaseResponse<InventoryItemDto>), 200)]
        [ProducesResponseType(typeof(BaseResponse), 400)]
        [ProducesResponseType(typeof(BaseResponse), 404)]
        [ProducesResponseType(typeof(BaseResponse), 409)]
        [ProducesResponseType(typeof(BaseResponse), 500)]
        public async Task<ActionResult<BaseResponse<InventoryItemDto>>> UpdateItem(int id, [FromBody] UpdateInventoryItemDto updateItemDto)
        {
            _logger.LogInformation("PUT /api/inventory/{ItemId} - Updating inventory item (Admin only)", id);

            if (!ModelState.IsValid)
            {
                var errors = string.Join(", ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                return BadRequest(BaseResponse.Error($"Validation failed: {errors}", 400));
            }

            var userId = GetCurrentUserId();
            if (userId <= 0)
            {
                _logger.LogWarning("Invalid user ID from JWT token");
                return Unauthorized(BaseResponse.Error("Invalid user authentication", 401));
            }

            var result = await _inventoryService.UpdateItemAsync(id, updateItemDto, userId);

            if (result.ErrorCode == 404)
                return NotFound(result);
            if (result.ErrorCode == 409)
                return Conflict(result);
            if (result.ErrorCode == 400)
                return BadRequest(result);
            if (result.ErrorCode != 0)
                return StatusCode(500, result);

            return Ok(result);
        }

        /// <summary>
        /// Delete an inventory item (Admin only)
        /// </summary>
        /// <param name="id">Item ID</param>
        /// <returns>Deletion result</returns>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(BaseResponse), 200)]
        [ProducesResponseType(typeof(BaseResponse), 404)]
        [ProducesResponseType(typeof(BaseResponse), 500)]
        public async Task<ActionResult<BaseResponse>> DeleteItem(int id)
        {
            _logger.LogInformation("DELETE /api/inventory/{ItemId} - Deleting inventory item (Admin only)", id);

            var userId = GetCurrentUserId();
            if (userId <= 0)
            {
                _logger.LogWarning("Invalid user ID from JWT token");
                return Unauthorized(BaseResponse.Error("Invalid user authentication", 401));
            }

            var result = await _inventoryService.DeleteItemAsync(id, userId);

            if (result.ErrorCode == 404)
                return NotFound(result);
            if (result.ErrorCode == 400)
                return BadRequest(result);
            if (result.ErrorCode != 0)
                return StatusCode(500, result);

            return Ok(result);
        }

        /// <summary>
        /// View all stock records (Admin only)
        /// </summary>
        /// <returns>List of all stock records with item details</returns>
        [HttpGet("stocks")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(BaseResponse<IEnumerable<StockDto>>), 200)]
        [ProducesResponseType(typeof(BaseResponse), 500)]
        public async Task<ActionResult<BaseResponse<IEnumerable<StockDto>>>> GetAllStock()
        {
            _logger.LogInformation("GET /api/inventory/stocks - Getting all stock records (Admin only)");

            var result = await _inventoryService.GetAllStockAsync();

            if (result.ErrorCode != 0)
                return StatusCode(500, result);

            return Ok(result);
        }

        /// <summary>
        /// Delete stock record for an item (Admin only)
        /// </summary>
        /// <param name="id">Item ID</param>
        /// <returns>Deletion result</returns>
        [HttpDelete("{id}/stock")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(BaseResponse), 200)]
        [ProducesResponseType(typeof(BaseResponse), 404)]
        [ProducesResponseType(typeof(BaseResponse), 500)]
        public async Task<ActionResult<BaseResponse>> DeleteStock(int id)
        {
            _logger.LogInformation("DELETE /api/inventory/{ItemId}/stock - Deleting stock for item (Admin only)", id);

            var userId = GetCurrentUserId();
            if (userId <= 0)
            {
                return Unauthorized(BaseResponse.Error("Invalid user authentication", 401));
            }

            var result = await _inventoryService.DeleteStockAsync(id, userId);

            if (result.ErrorCode == 404)
                return NotFound(result);
            if (result.ErrorCode != 0)
                return StatusCode(500, result);

            return Ok(result);
        }

        // Helper methods
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        private string GetCurrentUserRole()
        {
            return User.FindFirst("role")?.Value ?? "User";
        }

        private string GetCurrentUserName()
        {
            return User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
        }

        private string GetCurrentUserEmail()
        {
            return User.FindFirst(ClaimTypes.Email)?.Value ?? "";
        }
    }
}