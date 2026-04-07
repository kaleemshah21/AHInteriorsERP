using AH.Data;
using AHInteriorsERP.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

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

        private async Task LoadPageDataAsync()
        {
            ViewData["CustomerID"] = new SelectList(
                await _context.Customers.OrderBy(c => c.CustomerName).ToListAsync(),
                "CustomerID",
                "CustomerName"
            );

            var reservedLookup = await _context.OrderItems
                .AsNoTracking()
                .Where(oi => oi.Order != null && oi.Order.Status == OrderStatus.Pending)
                .GroupBy(oi => oi.ProductID)
                .Select(g => new
                {
                    ProductID = g.Key,
                    Reserved = g.Sum(x => x.Quantity)
                })
                .ToDictionaryAsync(x => x.ProductID, x => x.Reserved);

            var productEntities = await _context.Products
                .OrderBy(p => p.ProductName)
                .ToListAsync();

            Products = productEntities.Select(p =>
            {
                var reserved = reservedLookup.ContainsKey(p.ProductID) ? reservedLookup[p.ProductID] : 0;

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

            if (Items.Count == 0)
            {
                Items = Products.Select(p => new OrderItemInput
                {
                    ProductID = p.ProductID,
                    Quantity = 0
                }).ToList();
            }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadPageDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await LoadPageDataAsync();

            if (!ModelState.IsValid)
                return Page();

            var selected = Items.Where(i => i.Quantity > 0).ToList();

            if (selected.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "Please enter a quantity for at least one product.");
                return Page();
            }

            // Force new orders to Pending
            Order.Status = OrderStatus.Pending;

            // Check available stock
            foreach (var line in selected)
            {
                var product = Products.FirstOrDefault(p => p.ProductID == line.ProductID);

                if (product == null)
                {
                    ModelState.AddModelError(string.Empty, "One of the selected products could not be found.");
                    return Page();
                }

                if (line.Quantity > product.AvailableQuantity)
                {
                    ModelState.AddModelError(string.Empty,
                        $"Not enough available stock for {product.ProductName}. Available: {product.AvailableQuantity}.");
                    return Page();
                }
            }

            _context.Orders.Add(Order);
            await _context.SaveChangesAsync();

            var productIds = selected.Select(s => s.ProductID).ToList();
            var productLookup = await _context.Products
                .Where(p => productIds.Contains(p.ProductID))
                .ToDictionaryAsync(p => p.ProductID);

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