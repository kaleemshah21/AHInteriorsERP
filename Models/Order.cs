using System.ComponentModel.DataAnnotations;
// This model stores customer orders, including status, discount,
// related order items and calculated subtotal and total values.
namespace AHInteriorsERP.Models
{
    public class Order
    {
        public int OrderID { get; set; }

        // FK
        public int CustomerID { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        [Required]

        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        [StringLength(2000)]
        public string? Notes { get; set; }

        [Range(0, double.MaxValue)]
        public decimal DiscountAmount { get; set; } = 0m;

        // Navigation
        public Customer? Customer { get; set; }
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public Invoice? Invoice { get; set; }

        public decimal Subtotal
        {
            get
            {
                return OrderItems?.Sum(oi => oi.Quantity * oi.UnitPriceAtTime) ?? 0m;
            }
        }

        public decimal Total
        {
            get
            {
                var total = Subtotal - DiscountAmount;
                return total < 0 ? 0 : total;
            }
        }
    }
}
