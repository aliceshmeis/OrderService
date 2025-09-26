using Dapper;
using InventoryService.Domain.DTOs;
using InventoryService.Domain.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Npgsql;
using System.Data;

namespace InventoryService.Persistence.Repositories
{
    public class InventoryRepository : IInventoryRepository
    {
        private readonly IDbConnection _connection;
        private readonly ILogger<InventoryRepository> _logger;

        public InventoryRepository(IDbConnection connection, ILogger<InventoryRepository> logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public async Task<StoredProcedureResult<IEnumerable<InventoryItemDto>>> GetAllItemsAsync()
        {
            try
            {
                _logger.LogInformation("Executing sp_get_all_items");

                var result = await _connection.QuerySingleAsync<dynamic>(
                    "SELECT * FROM inventory.sp_get_all_items()"
                );

                var errorCode = (int)result.error_code;
                var data = result.data?.ToString();

                if (errorCode != 0 || string.IsNullOrEmpty(data))
                {
                    return new StoredProcedureResult<IEnumerable<InventoryItemDto>>
                    {
                        ErrorCode = errorCode,
                        Data = new List<InventoryItemDto>()
                    };
                }

                var items = JsonConvert.DeserializeObject<List<InventoryItemDto>>(data) ?? new List<InventoryItemDto>();

                return new StoredProcedureResult<IEnumerable<InventoryItemDto>>
                {
                    ErrorCode = errorCode,
                    Data = items
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing sp_get_all_items");
                return new StoredProcedureResult<IEnumerable<InventoryItemDto>>
                {
                    ErrorCode = 500,
                    Data = new List<InventoryItemDto>()
                };
            }
        }

        public async Task<StoredProcedureResult<InventoryItemDto>> GetItemByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation("Executing sp_get_item_by_id for ID: {ItemId}", id);

                var result = await _connection.QuerySingleAsync<dynamic>(
                    "SELECT * FROM inventory.sp_get_item_by_id(@p_id)",
                    new { p_id = id }
                );

                var errorCode = (int)result.error_code;
                var data = result.data?.ToString();

                InventoryItemDto? item = null;
                if (errorCode == 0 && !string.IsNullOrEmpty(data))
                {
                    item = JsonConvert.DeserializeObject<InventoryItemDto>(data);
                }

                return new StoredProcedureResult<InventoryItemDto>
                {
                    ErrorCode = errorCode,
                    Data = item
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing sp_get_item_by_id for ID: {ItemId}", id);
                return new StoredProcedureResult<InventoryItemDto>
                {
                    ErrorCode = 500,
                    Data = null
                };
            }
        }

        public async Task<StoredProcedureResult<InventoryItemDto>> CreateItemAsync(CreateInventoryItemDto createItemDto, int userId)
        {
            try
            {
                _logger.LogInformation("Executing sp_create_item for item: {ItemName}", createItemDto.ItemName);

                var result = await _connection.QuerySingleAsync<dynamic>(
                    "SELECT * FROM inventory.sp_create_item(@p_item_name, @p_item_code, @p_description, @p_category, @p_unit_price, @p_created_by, @p_initial_quantity, @p_warehouse_location)",
                    new
                    {
                        p_item_name = createItemDto.ItemName,
                        p_item_code = createItemDto.ItemCode,
                        p_description = createItemDto.Description,
                        p_category = createItemDto.Category,
                        p_unit_price = createItemDto.UnitPrice,
                        p_created_by = userId,
                        p_initial_quantity = createItemDto.InitialQuantity,
                        p_warehouse_location = createItemDto.WarehouseLocation
                    }
                );

                var errorCode = (int)result.error_code;
                var data = result.data?.ToString();

                InventoryItemDto? item = null;
                if (errorCode == 0 && !string.IsNullOrEmpty(data))
                {
                    item = JsonConvert.DeserializeObject<InventoryItemDto>(data);
                }

                return new StoredProcedureResult<InventoryItemDto>
                {
                    ErrorCode = errorCode,
                    Data = item
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing sp_create_item for item: {ItemName}", createItemDto.ItemName);
                return new StoredProcedureResult<InventoryItemDto>
                {
                    ErrorCode = 500,
                    Data = null
                };
            }
        }

        public async Task<StoredProcedureResult<InventoryItemDto>> UpdateItemAsync(int id, UpdateInventoryItemDto updateItemDto, int userId)
        {
            try
            {
                _logger.LogInformation("Executing sp_update_item for ID: {ItemId}", id);

                var result = await _connection.QuerySingleAsync<dynamic>(
                    "SELECT * FROM inventory.sp_update_item(@p_id, @p_item_name, @p_item_code, @p_description, @p_category, @p_unit_price, @p_is_active, @p_updated_by)",
                    new
                    {
                        p_id = id,
                        p_item_name = updateItemDto.ItemName,
                        p_item_code = updateItemDto.ItemCode,
                        p_description = updateItemDto.Description,
                        p_category = updateItemDto.Category,
                        p_unit_price = updateItemDto.UnitPrice,
                        p_is_active = updateItemDto.IsActive,
                        p_updated_by = userId
                    }
                );

                var errorCode = (int)result.error_code;
                var data = result.data?.ToString();

                InventoryItemDto? item = null;
                if (errorCode == 0 && !string.IsNullOrEmpty(data))
                {
                    item = JsonConvert.DeserializeObject<InventoryItemDto>(data);
                }

                return new StoredProcedureResult<InventoryItemDto>
                {
                    ErrorCode = errorCode,
                    Data = item
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing sp_update_item for ID: {ItemId}", id);
                return new StoredProcedureResult<InventoryItemDto>
                {
                    ErrorCode = 500,
                    Data = null
                };
            }
        }

        public async Task<StoredProcedureResult<string>> DeleteItemAsync(int id, int userId)
        {
            try
            {
                _logger.LogInformation("Executing sp_delete_item for ID: {ItemId}", id);

                var result = await _connection.QuerySingleAsync<dynamic>(
                    "SELECT * FROM inventory.sp_delete_item(@p_id, @p_updated_by)",
                    new
                    {
                        p_id = id,
                        p_updated_by = userId
                    }
                );

                var errorCode = (int)result.error_code;
                var data = result.data?.ToString();

                return new StoredProcedureResult<string>
                {
                    ErrorCode = errorCode,
                    Data = data
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing sp_delete_item for ID: {ItemId}", id);
                return new StoredProcedureResult<string>
                {
                    ErrorCode = 500,
                    Data = "Internal server error"
                };
            }
        }

        public async Task<StoredProcedureResult<IEnumerable<StockDto>>> GetAllStockAsync()
        {
            try
            {
                _logger.LogInformation("Executing sp_get_all_stock");

                var result = await _connection.QuerySingleAsync<dynamic>(
                    "SELECT * FROM inventory.sp_get_all_stock()"
                );

                var errorCode = (int)result.errorcode;
                var data = result.data?.ToString();

                if (errorCode != 0 || string.IsNullOrEmpty(data))
                {
                    return new StoredProcedureResult<IEnumerable<StockDto>>
                    {
                        ErrorCode = errorCode,
                        Data = new List<StockDto>()
                    };
                }

                var stocks = JsonConvert.DeserializeObject<List<StockDto>>(data) ?? new List<StockDto>();

                return new StoredProcedureResult<IEnumerable<StockDto>>
                {
                    ErrorCode = errorCode,
                    Data = stocks
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing sp_get_all_stock");
                return new StoredProcedureResult<IEnumerable<StockDto>>
                {
                    ErrorCode = 500,
                    Data = new List<StockDto>()
                };
            }
        }
        public async Task<StoredProcedureResult<string>> DeleteStockAsync(int itemId, int userId)
        {
            try
            {
                _logger.LogInformation("Executing sp_delete_stock for item ID: {ItemId}", itemId);

                var result = await _connection.QuerySingleAsync<dynamic>(
                    "SELECT * FROM inventory.sp_delete_stock(@p_item_id, @p_updated_by)",
                    new
                    {
                        p_item_id = itemId,
                        p_updated_by = userId
                    }
                );

                var errorCode = (int)result.error_code;
                var data = result.data?.ToString();

                return new StoredProcedureResult<string>
                {
                    ErrorCode = errorCode,
                    Data = data
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing sp_delete_stock for item ID: {ItemId}", itemId);
                return new StoredProcedureResult<string>
                {
                    ErrorCode = 500,
                    Data = "Internal server error"
                };
            }
        }
    }
}