using System.ComponentModel.DataAnnotations;

namespace AHInteriorsERP.Models
{
    public class Invoice
    {
        public int InvoiceID { get; set; }

        public int OrderID { get; set; }

        [Required]
        [StringLength(50)]
        public string InvoiceNumber { get; set; } = string.Empty;

        public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;

        [Range(0, double.MaxValue)]
        public decimal TotalAmount { get; set; }

        [StringLength(2000)]
        public string? Notes { get; set; }

        public Order? Order { get; set; }
        public ICollection<InvoiceItem> InvoiceItems { get; set; } = new List<InvoiceItem>();
    }
}