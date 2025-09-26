namespace InventoryService.Domain.Entities
{
    // Base entity with audit fields
    public abstract class BaseEntity
    {
        public int Id { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
    }

    // Items entity
    public class Item : BaseEntity
    {
        public string ItemName { get; set; } = string.Empty;
        public string ItemCode { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Category { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }

        // Navigation property
        public Stock? Stock { get; set; }
    }

    // Stock entity
    public class Stock : BaseEntity
    {
        public int ItemId { get; set; }
        public int QuantityAvailable { get; set; }
        public string? WarehouseLocation { get; set; }

        // Navigation property
        public Item Item { get; set; } = null!;
    }
}