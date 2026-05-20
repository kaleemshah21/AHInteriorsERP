using AH.Data;
using AHInteriorsERP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AHInteriorsERP.Pages.Orders
{
    [Authorize(Roles = "Admin,Staff,Warehouse")]
    public class EditModel : PageModel
    {

        private readonly AHInteriorsERPContext _context;

        public EditModel(AHInteriorsERPContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Order Order { get; set; } = default!;

        public List<ProductDisplayRow> Products { get; set; } = new();

        [BindProperty]
        public List<OrderItemInput> Items { get; set; } = new();

        public class OrderItemInput
        {
            public int ProductID { get; set; }
            public int Quantity { get; set; }
        }

        public class ProductDisplayRow
        {
            public int ProductID { get; set; }
            public string SKU { get; set; } = "";
            public string ProductName { get; set; } = "";
            public decimal BasePrice { get; set; }
            public int StockQuantity { get; set; }
            public int ReservedQuantity { get; set; }
            public int AvailableQuantity { get; set; }
        }

        private async Task LoadPageDataAsync(int orderId)
        {
            ViewData["CustomerID"] = new SelectList(
                await _context.Customers.OrderBy(c => c.CustomerName).ToListAsync(),
                "CustomerID",
                "CustomerName",
                Order.CustomerID
            );

            var reservedLookup = await _context.OrderItems
                .AsNoTracking()
                .Where(oi =>
                    oi.Order != null &&
                    oi.Order.Status == OrderStatus.Pending &&
                    oi.OrderID != orderId)
                .GroupBy(oi => oi.ProductID)
                .Select(g => new
                {
                    ProductID = g.Key,
                    Reserved = g.Sum(x => x.Quantity)
                })
                .ToDictionaryAsync(x => x.ProductID, x => x.Reserved);

            //gets current order products aswell, just in case a product has been disabled that is in a pending order

            var currentOrderProductIds = await _context.OrderItems
                .Where(oi => oi.OrderID == orderId)
                .Select(oi => oi.ProductID)
                .ToListAsync();

            var productEntities = await _context.Products
                .Where(p => p.isActive || currentOrderProductIds.Contains(p.ProductID))
                .OrderBy(p => p.ProductName)
                .ToListAsync();

            Products = productEntities.Select(p =>
            {
                var reserved = reservedLookup.ContainsKey(p.ProductID)
                    ? reservedLookup[p.ProductID]
                    : 0;

                return new ProductDisplayRow
                {
                    ProductID = p.ProductID,
                    SKU = p.SKU,
                    ProductName = p.ProductName,
                    BasePrice = p.BasePrice,
                    StockQuantity = p.StockQuantity,
                    ReservedQuantity = reserved,
                    AvailableQuantity = p.StockQuantity - reserved
                };
            }).ToList();
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(m => m.OrderID == id);

            if (order == null)
            {
                return NotFound();
            }

            Order = order;

            await LoadPageDataAsync(order.OrderID);

            Items = Products.Select(p =>
            {
                var existingItem = order.OrderItems.FirstOrDefault(oi => oi.ProductID == p.ProductID);

                return new OrderItemInput
                {
                    ProductID = p.ProductID,
                    Quantity = existingItem?.Quantity ?? 0
                };
            }).ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var existingOrder = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderID == Order.OrderID);

            if (existingOrder == null)
            {
                return NotFound();
            }

            var previousStatus = existingOrder.Status;

            await LoadPageDataAsync(existingOrder.OrderID);

            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (previousStatus == OrderStatus.Completed && Order.Status == OrderStatus.Pending)
            {
                ModelState.AddModelError(string.Empty, "A completed order cannot be changed back to pending.");
                return Page();
            }

            var selected = Items.Where(i => i.Quantity > 0).ToList();

            var subtotal = previousStatus == OrderStatus.Pending
                ? selected.Sum(i =>
                {
                    var product = Products.First(p => p.ProductID == i.ProductID);
                    return i.Quantity * product.BasePrice;
                })
                : existingOrder.OrderItems.Sum(i => i.Quantity * i.UnitPriceAtTime);

            if (Order.DiscountAmount < 0)
            {
                ModelState.AddModelError("Order.DiscountAmount", "Discount cannot be negative.");
                return Page();
            }

            if (Order.DiscountAmount > subtotal)
            {
                ModelState.AddModelError("Order.DiscountAmount", "Discount cannot be greater than the order subtotal.");
                return Page();
            }

            existingOrder.CustomerID = Order.CustomerID;
            existingOrder.OrderDate = Order.OrderDate;
            existingOrder.Status = Order.Status;
            existingOrder.Notes = Order.Notes;

            if (previousStatus == OrderStatus.Pending)
            {
                existingOrder.DiscountAmount = Order.DiscountAmount;
            }

            // Update order items only while pending
            if (previousStatus == OrderStatus.Pending)
            {
                if (selected.Count == 0)
                {
                    ModelState.AddModelError(string.Empty, "Please enter a quantity for at least one product.");
                    return Page();
                }

                foreach (var line in selected)
                {
                    var product = Products.FirstOrDefault(p => p.ProductID == line.ProductID);

                    if (product == null)
                    {
                        ModelState.AddModelError(string.Empty, "Product not found.");
                        return Page();
                    }

                    if (line.Quantity < 0)
                    {
                        ModelState.AddModelError(string.Empty, $"Quantity cannot be negative for {product.ProductName}.");
                        return Page();
                    }

                    if (line.Quantity > product.AvailableQuantity)
                    {
                        ModelState.AddModelError(string.Empty,
                            $"Not enough stock for {product.ProductName}. Available: {product.AvailableQuantity}");
                        return Page();
                    }
                }

                _context.OrderItems.RemoveRange(existingOrder.OrderItems);

                foreach (var line in selected)
                {
                    var product = await _context.Products.FirstAsync(p => p.ProductID == line.ProductID);

                    _context.OrderItems.Add(new OrderItem
                    {
                        OrderID = existingOrder.OrderID,
                        ProductID = product.ProductID,
                        Quantity = line.Quantity,
                        UnitPriceAtTime = product.BasePrice
                    });
                }

                await _context.SaveChangesAsync();

                existingOrder = await _context.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.OrderID == Order.OrderID);

                if (existingOrder == null)
                {
                    return NotFound();
                }
            }

            // If status changed to Completed, deduct stock and create invoice snapshot
            if (existingOrder.Status == OrderStatus.Completed &&
                previousStatus != OrderStatus.Completed)
            {
                var existingInvoice = await _context.Invoices
                    .FirstOrDefaultAsync(i => i.OrderID == existingOrder.OrderID);

                foreach (var orderItem in existingOrder.OrderItems)
                {
                    await _context.Entry(orderItem)
                        .Reference(oi => oi.Product)
                        .LoadAsync();
                }

                foreach (var item in existingOrder.OrderItems)
                {
                    if (item.Product == null)
                    {
                        ModelState.AddModelError(string.Empty, "A product on this order could not be found.");
                        return Page();
                    }

                    if (item.Product.StockQuantity < item.Quantity)
                    {
                        ModelState.AddModelError(string.Empty,
                            $"Not enough stock for {item.Product.ProductName}. Current stock: {item.Product.StockQuantity}.");
                        return Page();
                    }
                }

                foreach (var item in existingOrder.OrderItems)
                {
                    item.Product!.StockQuantity -= item.Quantity;
                }

                if (existingInvoice == null)
                {
                    var invoiceSubtotal = existingOrder.OrderItems.Sum(i => i.Quantity * i.UnitPriceAtTime);
                    var discount = existingOrder.DiscountAmount;
                    var total = invoiceSubtotal - discount;

                    var invoice = new Invoice
                    {
                        OrderID = existingOrder.OrderID,
                        InvoiceNumber = $"INV-{existingOrder.OrderID:00000}",
                        InvoiceDate = DateTime.UtcNow,
                        DiscountAmount = discount,
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

            // If completed order is cancelled, treat it as a return and add stock back
            if (previousStatus == OrderStatus.Completed &&
                existingOrder.Status == OrderStatus.Cancelled)
            {
                foreach (var orderItem in existingOrder.OrderItems)
                {
                    await _context.Entry(orderItem)
                        .Reference(oi => oi.Product)
                        .LoadAsync();
                }

                foreach (var item in existingOrder.OrderItems)
                {
                    if (item.Product != null)
                    {
                        item.Product.StockQuantity += item.Quantity;
                    }
                }

                existingOrder.Notes = string.IsNullOrWhiteSpace(existingOrder.Notes)
                    ? "Order cancelled after completion. Stock returned."
                    : existingOrder.Notes + "\nOrder cancelled after completion. Stock returned.";
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

                throw;
            }

            return RedirectToPage("./Index");
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.OrderID == id);
        }
    }
}