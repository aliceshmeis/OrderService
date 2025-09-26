using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrderService.Domain.Entities
{
    [Table("order_items", Schema = "order")]
    public class OrderItem
    {
        public int Id { get; set; }

        public int OrderId { get; set; }

        [Required]
        [StringLength(100)]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        public int Quantity { get; set; }

        [Required]
        public decimal Price { get; set; }

        [Required]
        public decimal UnitPrice { get; set; }

        [Required]
        public decimal TotalPrice { get; set; }

        // Audit Fields
        public int CreatedBy { get; set; } = 1;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;

        // Navigation Property
        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; } = null!;
    }
}