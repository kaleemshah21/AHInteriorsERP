using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AH.Data;
using AHInteriorsERP.Models;

namespace AHInteriorsERP.Pages.Products
{
    public class IndexModel : PageModel
    {
        private readonly AHInteriorsERPContext _context;

        public IndexModel(AHInteriorsERPContext context)
        {
            _context = context;
        }

        public IList<ProductRow> Products { get; set; } = new List<ProductRow>();

        public class ProductRow
        {
            public int ProductID { get; set; }
            public string SKU { get; set; } = "";
            public string ProductName { get; set; } = "";
            public string? Description { get; set; }
            public decimal BasePrice { get; set; }
            public int StockQuantity { get; set; }
            public int ReservedQuantity { get; set; }
            public int AvailableQuantity { get; set; }
            public string? LocationCode { get; set; }
            public bool IsActive { get; set; }
        }

        public async Task OnGetAsync()
        {
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

            var products = await _context.Products
                .AsNoTracking()
                .OrderBy(p => p.ProductName)
                .ToListAsync();

            Products = products.Select(p =>
            {
                var reserved = reservedLookup.ContainsKey(p.ProductID) ? reservedLookup[p.ProductID] : 0;

                return new ProductRow
                {
                    ProductID = p.ProductID,
                    SKU = p.SKU,
                    ProductName = p.ProductName,
                    Description = p.Description,
                    BasePrice = p.BasePrice,
                    StockQuantity = p.StockQuantity,
                    ReservedQuantity = reserved,
                    AvailableQuantity = p.StockQuantity - reserved,
                    LocationCode = p.LocationCode,
                    
                };
            }).ToList();
        }
    }
}