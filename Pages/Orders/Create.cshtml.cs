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

        [BindProperty]
        public NewCustomerInput NewCustomer { get; set; } = new();

        [BindProperty]
        public bool CreateNewCustomer { get; set; }

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

        public class NewCustomerInput
        {
            public string CustomerName { get; set; } = string.Empty;
            public string? Phone { get; set; }
            public string? Email { get; set; }
            public string? AddressLine1 { get; set; }
            public string? AddressLine2 { get; set; }
            public string? City { get; set; }
            public string? Postcode { get; set; }
        }

        private async Task LoadPageDataAsync()
        {
            var customers = await _context.Customers
                .OrderBy(c => c.CustomerName)
                .Select(c => new
                {
                    c.CustomerID,
                    Display = c.CustomerName + " (" + (c.Postcode ?? "No postcode") + ")"
                })
                .ToListAsync();

            ViewData["CustomerID"] = new SelectList(customers, "CustomerID", "Display", Order.CustomerID);

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

            if (CreateNewCustomer)
            {
                if (string.IsNullOrWhiteSpace(NewCustomer.CustomerName))
                {
                    ModelState.AddModelError("NewCustomer.CustomerName", "Customer name is required.");
                }

                if (!string.IsNullOrWhiteSpace(NewCustomer.Email) &&
                    !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(NewCustomer.Email))
                {
                    ModelState.AddModelError("NewCustomer.Email", "Please enter a valid email address.");
                }

                if (!string.IsNullOrWhiteSpace(NewCustomer.Phone) &&
                    !new System.ComponentModel.DataAnnotations.PhoneAttribute().IsValid(NewCustomer.Phone))
                {
                    ModelState.AddModelError("NewCustomer.Phone", "Please enter a valid phone number.");
                }
            }
            else
            {
                if (Order.CustomerID <= 0)
                {
                    ModelState.AddModelError("Order.CustomerID", "Please select a customer.");
                }
            }

            var selected = Items.Where(i => i.Quantity > 0).ToList();

            if (selected.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "Please enter a quantity for at least one product.");
            }

            foreach (var line in selected)
            {
                var product = Products.FirstOrDefault(p => p.ProductID == line.ProductID);

                if (product == null)
                {
                    ModelState.AddModelError(string.Empty, "One of the selected products could not be found.");
                    break;
                }

                if (line.Quantity > product.AvailableQuantity)
                {
                    ModelState.AddModelError(string.Empty,
                        $"Not enough available stock for {product.ProductName}. Available: {product.AvailableQuantity}.");
                }

                if (line.Quantity < 0)
                {
                    ModelState.AddModelError(string.Empty,
                        $"Quantity cannot be negative for {product.ProductName}.");
                }
            }

            if (!ModelState.IsValid)
                return Page();

            if (CreateNewCustomer)
            {
                var customer = new Customer
                {
                    CustomerName = NewCustomer.CustomerName.Trim(),
                    Phone = string.IsNullOrWhiteSpace(NewCustomer.Phone) ? null : NewCustomer.Phone.Trim(),
                    Email = string.IsNullOrWhiteSpace(NewCustomer.Email) ? null : NewCustomer.Email.Trim(),
                    AddressLine1 = string.IsNullOrWhiteSpace(NewCustomer.AddressLine1) ? null : NewCustomer.AddressLine1.Trim(),
                    AddressLine2 = string.IsNullOrWhiteSpace(NewCustomer.AddressLine2) ? null : NewCustomer.AddressLine2.Trim(),
                    City = string.IsNullOrWhiteSpace(NewCustomer.City) ? null : NewCustomer.City.Trim(),
                    Postcode = string.IsNullOrWhiteSpace(NewCustomer.Postcode) ? null : NewCustomer.Postcode.Trim(),
                    CreatedAt = DateTime.UtcNow
                };

                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();

                Order.CustomerID = customer.CustomerID;
            }

            Order.Status = OrderStatus.Pending;

            if (Order.OrderDate == default)
            {
                Order.OrderDate = DateTime.UtcNow;
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