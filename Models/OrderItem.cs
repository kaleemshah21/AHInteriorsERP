using System.ComponentModel.DataAnnotations;

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
