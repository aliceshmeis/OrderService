using System.ComponentModel.DataAnnotations;

namespace InventoryService.Domain.DTOs
{
    // Base DTO for inventory items
    public class InventoryItemDto
    {
        public int Id { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string ItemCode { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Category { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int QuantityAvailable { get; set; }
        public string? WarehouseLocation { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
    }

    // DTO for creating new inventory items
    public class CreateInventoryItemDto
    {
        [Required(ErrorMessage = "Item name is required")]
        [StringLength(100, ErrorMessage = "Item name cannot exceed 100 characters")]
        public string ItemName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Item code is required")]
        [StringLength(50, ErrorMessage = "Item code cannot exceed 50 characters")]
        public string ItemCode { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Category is required")]
        [StringLength(50, ErrorMessage = "Category cannot exceed 50 characters")]
        public string Category { get; set; } = string.Empty;

        [Required(ErrorMessage = "Unit price is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Unit price must be greater than 0")]
        public decimal UnitPrice { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Initial quantity cannot be negative")]
        public int InitialQuantity { get; set; } = 0;

        [StringLength(100, ErrorMessage = "Warehouse location cannot exceed 100 characters")]
        public string? WarehouseLocation { get; set; }

       
    }

    // DTO for updating inventory items
    public class UpdateInventoryItemDto
    {
        [Required(ErrorMessage = "Item name is required")]
        [StringLength(100, ErrorMessage = "Item name cannot exceed 100 characters")]
        public string ItemName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Item code is required")]
        [StringLength(50, ErrorMessage = "Item code cannot exceed 50 characters")]
        public string ItemCode { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Category is required")]
        [StringLength(50, ErrorMessage = "Category cannot exceed 50 characters")]
        public string Category { get; set; } = string.Empty;

        [Required(ErrorMessage = "Unit price is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Unit price must be greater than 0")]
        public decimal UnitPrice { get; set; }

        public bool IsActive { get; set; } = true;

        // This will be set from JWT token
    }

    // DTO for stock operations
    public class StockDto
    {
        public int Id { get; set; }
        public int ItemId { get; set; }
        public string? ItemName { get; set; }
        public string? ItemCode { get; set; }
        public int QuantityAvailable { get; set; }
        public string? WarehouseLocation { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
    }

    // DTO for updating stock
    public class UpdateStockDto
    {
        [Required(ErrorMessage = "Item ID is required")]
        public int ItemId { get; set; }

        [Required(ErrorMessage = "Quantity is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Quantity cannot be negative")]
        public int QuantityAvailable { get; set; }

        [StringLength(100, ErrorMessage = "Warehouse location cannot exceed 100 characters")]
        public string? WarehouseLocation { get; set; }

        // This will be set from JWT token
        public int UpdatedBy { get; set; }
    }

    // DTO for adjusting stock (add/subtract quantities)
    public class AdjustStockDto
    {
        [Required(ErrorMessage = "Item ID is required")]
        public int ItemId { get; set; }

        [Required(ErrorMessage = "Quantity change is required")]
        public int QuantityChange { get; set; } // Positive to add, negative to subtract

        public string? Reason { get; set; } // Optional reason for the adjustment

        // This will be set from JWT token
        public int UpdatedBy { get; set; }
    }
    // Add this to your DTOs file
    public class StockSummaryDto
    {
        public int TotalItems { get; set; }
        public int ItemsInStock { get; set; }
        public int ItemsOutOfStock { get; set; }
        public int LowStockItems { get; set; }
        public decimal TotalInventoryValue { get; set; }
    }
}