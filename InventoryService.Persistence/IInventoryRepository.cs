using InventoryService.Domain.DTOs;
using InventoryService.Domain.Models;

namespace InventoryService.Persistence.Repositories
{
    public interface IInventoryRepository
    {
        // Item CRUD operations - matching your 5 stored procedures
        Task<StoredProcedureResult<IEnumerable<InventoryItemDto>>> GetAllItemsAsync();
        Task<StoredProcedureResult<InventoryItemDto>> GetItemByIdAsync(int id);
        Task<StoredProcedureResult<InventoryItemDto>> CreateItemAsync(CreateInventoryItemDto createItemDto, int userId);
        Task<StoredProcedureResult<InventoryItemDto>> UpdateItemAsync(int id, UpdateInventoryItemDto updateItemDto, int userId);
        Task<StoredProcedureResult<string>> DeleteItemAsync(int id, int userId);

        // Stock operations
        Task<StoredProcedureResult<IEnumerable<StockDto>>> GetAllStockAsync();
        Task<StoredProcedureResult<string>> DeleteStockAsync(int itemId, int userId);
    }
}