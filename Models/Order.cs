using System.ComponentModel.DataAnnotations;

namespace AHInteriorsERP.Models
{
    public class Order
    {
        public int OrderID { get; set; }

        // FK
        public int CustomerID { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending";

        [StringLength(2000)]
        public string? Notes { get; set; }

        // Navigation
        public Customer? Customer { get; set; }
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public Invoice? Invoice { get; set; }
    }
}
