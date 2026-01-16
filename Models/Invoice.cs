using System.ComponentModel.DataAnnotations;

namespace AHInteriorsERP.Models
{
    public class Invoice
    {
        public int InvoiceID { get; set; }

        // FK (one-to-one with Order)
        public int OrderID { get; set; }

        [Required]
        [StringLength(50)]
        public string InvoiceNumber { get; set; } = string.Empty;

        public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;

        [Range(0, double.MaxValue)]
        public decimal TotalAmount { get; set; }

        [Required]
        [StringLength(50)]
        public string PaymentStatus { get; set; } = "Unpaid";

        [StringLength(2000)]
        public string? Notes { get; set; }

        // Navigation
        public Order? Order { get; set; }
    }
}
