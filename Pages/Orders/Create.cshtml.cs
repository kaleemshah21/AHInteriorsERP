using AH.Data;
using AHInteriorsERP.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AHInteriorsERP.Pages.Orders
{
    public class CreateModel : PageModel
    {
        private readonly AHInteriorsERPContext _context;

        public CreateModel(AHInteriorsERPContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Order Order { get; set; } = new Order();

        public List<Product> Products { get; set; } = new();

        [BindProperty]
        public List<OrderItemInput> Items { get; set; } = new();

        public class OrderItemInput
        {
            public int ProductID { get; set; }
            public int Quantity { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            ViewData["CustomerID"] = new SelectList(
                await _context.Customers.OrderBy(c => c.CustomerName).ToListAsync(),
                "CustomerID",
                "CustomerName"
            );

            Products = await _context.Products
                .OrderBy(p => p.ProductName)
                .ToListAsync();

            Items = Products.Select(p => new OrderItemInput
            {
                ProductID = p.ProductID,
                Quantity = 0
            }).ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Reload dropdown + products so the page can re-render on validation errors
            ViewData["CustomerID"] = new SelectList(
                await _context.Customers.OrderBy(c => c.CustomerName).ToListAsync(),
                "CustomerID",
                "CustomerName"
            );

            Products = await _context.Products
                .OrderBy(p => p.ProductName)
                .ToListAsync();

            if (!ModelState.IsValid)
                return Page();

            var selected = Items.Where(i => i.Quantity > 0).ToList();
            if (selected.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "Please enter a quantity for at least one product.");
                return Page();
            }

            // Create order header
            _context.Orders.Add(Order);
            await _context.SaveChangesAsync(); // generates OrderID

            // Load products for prices
            var productIds = selected.Select(s => s.ProductID).ToList();
            var productLookup = await _context.Products
                .Where(p => productIds.Contains(p.ProductID))
                .ToDictionaryAsync(p => p.ProductID);

            // Create order items
            foreach (var line in selected)
            {
                var p = productLookup[line.ProductID];

                _context.OrderItems.Add(new OrderItem
                {
                    OrderID = Order.OrderID,
                    ProductID = p.ProductID,
                    Quantity = line.Quantity,
                    UnitPriceAtTime = p.BasePrice
                });
            }

            await _context.SaveChangesAsync();

            return RedirectToPage("./Details", new { id = Order.OrderID });
        }
    }
}
