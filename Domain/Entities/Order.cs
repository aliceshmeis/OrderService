using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrderService.Domain.Entities
{
    [Table("orders", Schema = "order")]
    public class Order
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string OrderCode { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string CustomerName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string CustomerEmail { get; set; } = string.Empty;

        [Required]
        public decimal TotalAmount { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        // Audit Fields
        public int CreatedBy { get; set; } = 1;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;

        // Navigation Property
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}