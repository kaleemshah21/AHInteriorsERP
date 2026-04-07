using System.ComponentModel.DataAnnotations;

namespace AHInteriorsERP.Models
{
    public class InvoiceItem
    {
        public int InvoiceItemID { get; set; }

        public int InvoiceID { get; set; }

        [Required]
        [StringLength(200)]
        public string ProductName { get; set; } = string.Empty;

        [StringLength(100)]
        public string? SKU { get; set; }

        public int Quantity { get; set; }

        [Range(0, double.MaxValue)]
        public decimal UnitPrice { get; set; }

        [Range(0, double.MaxValue)]
        public decimal LineTotal { get; set; }

        public Invoice? Invoice { get; set; }
    }
}