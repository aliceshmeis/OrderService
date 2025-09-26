using InventoryService.Domain.DTOs;
using InventoryService.Domain.Models;
using InventoryService.Persistence.UnitOfWork;
using Microsoft.Extensions.Logging;

namespace InventoryService.Business.Services
{
    public class InventoryServiceImpl : IInventoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<InventoryServiceImpl> _logger;

        public InventoryServiceImpl(IUnitOfWork unitOfWork, ILogger<InventoryServiceImpl> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<BaseResponse<IEnumerable<InventoryItemDto>>> GetAllItemsAsync()
        {
            try
            {
                _logger.LogInformation("Getting all inventory items");

                var result = await _unitOfWork.InventoryRepository.GetAllItemsAsync();

                if (result.ErrorCode != 0)
                {
                    _logger.LogWarning("Failed to get all items. ErrorCode: {ErrorCode}", result.ErrorCode);
                    return BaseResponse<IEnumerable<InventoryItemDto>>.Error(
                        "Failed to retrieve inventory items",
                        result.ErrorCode
                    );
                }

                _logger.LogInformation("Successfully retrieved {ItemCount} inventory items", result.Data?.Count() ?? 0);
                return BaseResponse<IEnumerable<InventoryItemDto>>.Success(
                    result.Data ?? new List<InventoryItemDto>(),
                    "Inventory items retrieved successfully"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting all inventory items");
                return BaseResponse<IEnumerable<InventoryItemDto>>.Error(
                    "An error occurred while retrieving inventory items",
                    500
                );
            }
        }

        public async Task<BaseResponse<InventoryItemDto>> GetItemByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation("Getting inventory item by ID: {ItemId}", id);

                if (id <= 0)
                {
                    _logger.LogWarning("Invalid item ID: {ItemId}", id);
                    return BaseResponse<InventoryItemDto>.Error("Invalid item ID", 400);
                }

                var result = await _unitOfWork.InventoryRepository.GetItemByIdAsync(id);

                if (result.ErrorCode == 404)
                {
                    _logger.LogWarning("Item not found with ID: {ItemId}", id);
                    return BaseResponse<InventoryItemDto>.Error("Item not found", 404);
                }

                if (result.ErrorCode != 0)
                {
                    _logger.LogWarning("Failed to get item with ID: {ItemId}. ErrorCode: {ErrorCode}", id, result.ErrorCode);
                    return BaseResponse<InventoryItemDto>.Error(
                        "Failed to retrieve inventory item",
                        result.ErrorCode
                    );
                }

                _logger.LogInformation("Successfully retrieved inventory item with ID: {ItemId}", id);
                return BaseResponse<InventoryItemDto>.Success(
                    result.Data!,
                    "Inventory item retrieved successfully"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting inventory item by ID: {ItemId}", id);
                return BaseResponse<InventoryItemDto>.Error(
                    "An error occurred while retrieving the inventory item",
                    500
                );
            }
        }

        public async Task<BaseResponse<InventoryItemDto>> CreateItemAsync(CreateInventoryItemDto createItemDto, int userId)
        {
            try
            {
                _logger.LogInformation("Creating new inventory item: {ItemName}", createItemDto.ItemName);

                // Additional business validation can go here
                if (string.IsNullOrWhiteSpace(createItemDto.ItemName))
                {
                    return BaseResponse<InventoryItemDto>.Error("Item name is required", 400);
                }

                if (string.IsNullOrWhiteSpace(createItemDto.ItemCode))
                {
                    return BaseResponse<InventoryItemDto>.Error("Item code is required", 400);
                }

                if (createItemDto.UnitPrice <= 0)
                {
                    return BaseResponse<InventoryItemDto>.Error("Unit price must be greater than 0", 400);
                }

                var result = await _unitOfWork.InventoryRepository.CreateItemAsync(createItemDto, userId);

                if (result.ErrorCode == 409)
                {
                    _logger.LogWarning("Item code already exists: {ItemCode}", createItemDto.ItemCode);
                    return BaseResponse<InventoryItemDto>.Error("Item code already exists", 409);
                }

                if (result.ErrorCode != 0)
                {
                    _logger.LogWarning("Failed to create item: {ItemName}. ErrorCode: {ErrorCode}", createItemDto.ItemName, result.ErrorCode);
                    return BaseResponse<InventoryItemDto>.Error(
                        "Failed to create inventory item",
                        result.ErrorCode
                    );
                }

                _logger.LogInformation("Successfully created inventory item: {ItemName} with ID: {ItemId}",
                    createItemDto.ItemName, result.Data?.Id);

                return BaseResponse<InventoryItemDto>.Success(
                    result.Data!,
                    "Inventory item created successfully"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating inventory item: {ItemName}", createItemDto.ItemName);
                return BaseResponse<InventoryItemDto>.Error(
                    "An error occurred while creating the inventory item",
                    500
                );
            }
        }

        public async Task<BaseResponse<InventoryItemDto>> UpdateItemAsync(int id, UpdateInventoryItemDto updateItemDto, int userId)
        {
            try
            {
                _logger.LogInformation("Updating inventory item with ID: {ItemId}", id);

                if (id <= 0)
                {
                    _logger.LogWarning("Invalid item ID: {ItemId}", id);
                    return BaseResponse<InventoryItemDto>.Error("Invalid item ID", 400);
                }

                // Additional business validation can go here
                if (string.IsNullOrWhiteSpace(updateItemDto.ItemName))
                {
                    return BaseResponse<InventoryItemDto>.Error("Item name is required", 400);
                }

                if (string.IsNullOrWhiteSpace(updateItemDto.ItemCode))
                {
                    return BaseResponse<InventoryItemDto>.Error("Item code is required", 400);
                }

                if (updateItemDto.UnitPrice <= 0)
                {
                    return BaseResponse<InventoryItemDto>.Error("Unit price must be greater than 0", 400);
                }

                var result = await _unitOfWork.InventoryRepository.UpdateItemAsync(id, updateItemDto, userId);

                if (result.ErrorCode == 404)
                {
                    _logger.LogWarning("Item not found with ID: {ItemId}", id);
                    return BaseResponse<InventoryItemDto>.Error("Item not found", 404);
                }

                if (result.ErrorCode == 409)
                {
                    _logger.LogWarning("Item code already exists: {ItemCode}", updateItemDto.ItemCode);
                    return BaseResponse<InventoryItemDto>.Error("Item code already exists", 409);
                }

                if (result.ErrorCode != 0)
                {
                    _logger.LogWarning("Failed to update item with ID: {ItemId}. ErrorCode: {ErrorCode}", id, result.ErrorCode);
                    return BaseResponse<InventoryItemDto>.Error(
                        "Failed to update inventory item",
                        result.ErrorCode
                    );
                }

                _logger.LogInformation("Successfully updated inventory item with ID: {ItemId}", id);
                return BaseResponse<InventoryItemDto>.Success(
                    result.Data!,
                    "Inventory item updated successfully"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating inventory item with ID: {ItemId}", id);
                return BaseResponse<InventoryItemDto>.Error(
                    "An error occurred while updating the inventory item",
                    500
                );
            }
        }

        public async Task<BaseResponse> DeleteItemAsync(int id, int userId)
        {
            try
            {
                _logger.LogInformation("Deleting inventory item with ID: {ItemId} by user: {UserId}", id, userId);

                if (id <= 0)
                {
                    _logger.LogWarning("Invalid item ID: {ItemId}", id);
                    return BaseResponse.Error("Invalid item ID", 400);
                }

                if (userId <= 0)
                {
                    _logger.LogWarning("Invalid user ID: {UserId}", userId);
                    return BaseResponse.Error("Invalid user ID", 400);
                }

                var result = await _unitOfWork.InventoryRepository.DeleteItemAsync(id, userId);

                if (result.ErrorCode == 404)
                {
                    _logger.LogWarning("Item not found with ID: {ItemId}", id);
                    return BaseResponse.Error("Item not found", 404);
                }

                if (result.ErrorCode != 0)
                {
                    _logger.LogWarning("Failed to delete item with ID: {ItemId}. ErrorCode: {ErrorCode}", id, result.ErrorCode);
                    return BaseResponse.Error(
                        "Failed to delete inventory item",
                        result.ErrorCode
                    );
                }

                _logger.LogInformation("Successfully deleted inventory item with ID: {ItemId}", id);
                return BaseResponse.Success("Inventory item deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting inventory item with ID: {ItemId}", id);
                return BaseResponse.Error(
                    "An error occurred while deleting the inventory item",
                    500
                );
            }
        }

        public async Task<BaseResponse<IEnumerable<StockDto>>> GetAllStockAsync()
        {
            try
            {
                _logger.LogInformation("Getting all stock records");

                var result = await _unitOfWork.InventoryRepository.GetAllStockAsync();

                if (result.ErrorCode != 0)
                {
                    _logger.LogWarning("Failed to get all stock records. ErrorCode: {ErrorCode}", result.ErrorCode);
                    return BaseResponse<IEnumerable<StockDto>>.Error(
                        "Failed to retrieve stock records",
                        result.ErrorCode
                    );
                }

                _logger.LogInformation("Successfully retrieved {StockCount} stock records", result.Data?.Count() ?? 0);
                return BaseResponse<IEnumerable<StockDto>>.Success(
                    result.Data ?? new List<StockDto>(),
                    "Stock records retrieved successfully"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting all stock records");
                return BaseResponse<IEnumerable<StockDto>>.Error(
                    "An error occurred while retrieving stock records",
                    500
                );
            }
        }

        public async Task<BaseResponse> DeleteStockAsync(int itemId, int userId)
        {
            try
            {
                _logger.LogInformation("Deleting stock for item ID: {ItemId} by user: {UserId}", itemId, userId);

                if (itemId <= 0)
                {
                    _logger.LogWarning("Invalid item ID: {ItemId}", itemId);
                    return BaseResponse.Error("Invalid item ID", 400);
                }

                if (userId <= 0)
                {
                    _logger.LogWarning("Invalid user ID: {UserId}", userId);
                    return BaseResponse.Error("Invalid user ID", 400);
                }

                var result = await _unitOfWork.InventoryRepository.DeleteStockAsync(itemId, userId);

                if (result.ErrorCode == 404)
                {
                    _logger.LogWarning("Stock not found for item ID: {ItemId}", itemId);
                    return BaseResponse.Error("Stock not found", 404);
                }

                if (result.ErrorCode != 0)
                {
                    _logger.LogWarning("Failed to delete stock for item ID: {ItemId}. ErrorCode: {ErrorCode}", itemId, result.ErrorCode);
                    return BaseResponse.Error(
                        "Failed to delete stock",
                        result.ErrorCode
                    );
                }

                _logger.LogInformation("Successfully deleted stock for item ID: {ItemId}", itemId);
                return BaseResponse.Success("Stock deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting stock for item ID: {ItemId}", itemId);
                return BaseResponse.Error(
                    "An error occurred while deleting the stock",
                    500
                );
            }
        }
    }
}