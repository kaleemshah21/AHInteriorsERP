using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AH.Data;
using AHInteriorsERP.Models;

namespace AHInteriorsERP.Pages.Orders
{
    public class EditModel : PageModel
    {
        private readonly AHInteriorsERPContext _context;

        public EditModel(AHInteriorsERPContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Order Order { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .FirstOrDefaultAsync(m => m.OrderID == id);

            if (order == null)
            {
                return NotFound();
            }

            Order = order;
            ViewData["CustomerID"] = new SelectList(_context.Customers, "CustomerID", "CustomerName");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                ViewData["CustomerID"] = new SelectList(_context.Customers, "CustomerID", "CustomerName");
                return Page();
            }

            var existingOrder = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderID == Order.OrderID);

            if (existingOrder == null)
            {
                return NotFound();
            }

            var previousStatus = existingOrder.Status;

            // Update editable fields
            existingOrder.CustomerID = Order.CustomerID;
            existingOrder.OrderDate = Order.OrderDate;
            existingOrder.Status = Order.Status;
            existingOrder.Notes = Order.Notes;

            // If status changed to Completed, create invoice if one does not already exist
            if (existingOrder.Status == OrderStatus.Completed &&
                previousStatus != OrderStatus.Completed)
            {
                var existingInvoice = await _context.Invoices
                    .FirstOrDefaultAsync(i => i.OrderID == existingOrder.OrderID);

                if (existingInvoice == null)
                {
                    var total = existingOrder.OrderItems.Sum(i => i.Quantity * i.UnitPriceAtTime);

                    var invoice = new Invoice
                    {
                        OrderID = existingOrder.OrderID,
                        InvoiceNumber = $"INV-{existingOrder.OrderID:00000}",
                        InvoiceDate = DateTime.UtcNow,
                        TotalAmount = total,
                        PaymentStatus = "Unpaid",
                        Notes = $"Auto-generated when order {existingOrder.OrderID} was marked as completed."
                    };

                    _context.Invoices.Add(invoice);
                }
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!OrderExists(Order.OrderID))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("./Index");
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.OrderID == id);
        }
    }
}