using System.ComponentModel.DataAnnotations;

namespace AHInteriorsERP.Models
{
    public class Product
    {
        public int ProductID { get; set; }

        [Required]
        [StringLength(50)]
        public string SKU { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string ProductName { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Description { get; set; }

        public int StockQuantity { get; set; }

        [Range(0, double.MaxValue)]
        public decimal BasePrice { get; set; }


        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation: one Product can appear in many OrderItems
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
