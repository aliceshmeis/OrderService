using InventoryService.Domain.DTOs;
using InventoryService.Domain.Models;

namespace InventoryService.Business.Services
{
    public interface IInventoryService
    {
        Task<BaseResponse<IEnumerable<InventoryItemDto>>> GetAllItemsAsync();
        Task<BaseResponse<InventoryItemDto>> GetItemByIdAsync(int id);
        Task<BaseResponse<InventoryItemDto>> CreateItemAsync(CreateInventoryItemDto createItemDto, int userId);
        Task<BaseResponse<InventoryItemDto>> UpdateItemAsync(int id, UpdateInventoryItemDto updateItemDto, int userId);
        Task<BaseResponse> DeleteItemAsync(int id, int userId);

        // Add these new methods for the controller
        Task<BaseResponse<IEnumerable<StockDto>>> GetAllStockAsync();
        Task<BaseResponse> DeleteStockAsync(int itemId, int userId);
    }
}