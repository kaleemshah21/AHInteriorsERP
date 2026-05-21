using System.ComponentModel.DataAnnotations;
// This model stores the products selected in an order,
// including quantity and the product price at the time of ordering.
namespace AHInteriorsERP.Models
{
    public class OrderItem
    {
        public int OrderItemID { get; set; }

        // FKs
        public int OrderID { get; set; }
        public int ProductID { get; set; }

        public int Quantity { get; set; }

        [Range(0, double.MaxValue)]
        public decimal UnitPriceAtTime { get; set; }

        // Navigation
        public Order? Order { get; set; }
        public Product? Product { get; set; }
    }
}
