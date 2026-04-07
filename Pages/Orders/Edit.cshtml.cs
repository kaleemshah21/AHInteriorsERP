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

            // Block reversing a completed order
            if (previousStatus == OrderStatus.Completed && Order.Status == OrderStatus.Pending)
            {
                ModelState.AddModelError(string.Empty, "A completed order cannot be changed back to pending.");
                ViewData["CustomerID"] = new SelectList(_context.Customers, "CustomerID", "CustomerName");
                return Page();
            }

            if (previousStatus == OrderStatus.Completed && Order.Status == OrderStatus.Cancelled)
            {
                ModelState.AddModelError(string.Empty, "A completed order cannot be cancelled once an invoice has been created.");
                ViewData["CustomerID"] = new SelectList(_context.Customers, "CustomerID", "CustomerName");
                return Page();
            }

            // Update editable fields
            existingOrder.CustomerID = Order.CustomerID;
            existingOrder.OrderDate = Order.OrderDate;
            existingOrder.Status = Order.Status;
            existingOrder.Notes = Order.Notes;

            // If status changed to Completed, deduct stock and create invoice snapshot if one does not already exist
            if (existingOrder.Status == OrderStatus.Completed &&
                previousStatus != OrderStatus.Completed)
            {
                var existingInvoice = await _context.Invoices
                    .FirstOrDefaultAsync(i => i.OrderID == existingOrder.OrderID);

                // Load products
                foreach (var orderItem in existingOrder.OrderItems)
                {
                    await _context.Entry(orderItem)
                        .Reference(oi => oi.Product)
                        .LoadAsync();
                }

                // Check stock first
                foreach (var item in existingOrder.OrderItems)
                {
                    if (item.Product == null)
                    {
                        ModelState.AddModelError(string.Empty, "A product on this order could not be found.");
                        ViewData["CustomerID"] = new SelectList(_context.Customers, "CustomerID", "CustomerName");
                        return Page();
                    }

                    if (item.Product.StockQuantity < item.Quantity)
                    {
                        ModelState.AddModelError(string.Empty,
                            $"Not enough stock for {item.Product.ProductName}. Current stock: {item.Product.StockQuantity}.");
                        ViewData["CustomerID"] = new SelectList(_context.Customers, "CustomerID", "CustomerName");
                        return Page();
                    }
                }

                // Deduct stock
                foreach (var item in existingOrder.OrderItems)
                {
                    item.Product!.StockQuantity -= item.Quantity;
                }

                // Create invoice snapshot if missing
                if (existingInvoice == null)
                {
                    var total = existingOrder.OrderItems.Sum(i => i.Quantity * i.UnitPriceAtTime);

                    var invoice = new Invoice
                    {
                        OrderID = existingOrder.OrderID,
                        InvoiceNumber = $"INV-{existingOrder.OrderID:00000}",
                        InvoiceDate = DateTime.UtcNow,
                        TotalAmount = total,
                        Notes = "Thank you for your shopping with us."
                    };

                    _context.Invoices.Add(invoice);
                    await _context.SaveChangesAsync();

                    foreach (var item in existingOrder.OrderItems)
                    {
                        _context.InvoiceItems.Add(new InvoiceItem
                        {
                            InvoiceID = invoice.InvoiceID,
                            ProductName = item.Product?.ProductName ?? "Unknown Product",
                            SKU = item.Product?.SKU,
                            Quantity = item.Quantity,
                            UnitPrice = item.UnitPriceAtTime,
                            LineTotal = item.Quantity * item.UnitPriceAtTime
                        });
                    }
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