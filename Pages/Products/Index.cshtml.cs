using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
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

        public PaginatedList<ProductRow> Products { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public string? SearchString { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SortOrder { get; set; }

        public string CurrentFilter { get; set; } = "";
        public string NameSort { get; set; } = "";
        public string PriceSort { get; set; } = "";
        public string StockSort { get; set; } = "";

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

        public async Task OnGetAsync(string? sortOrder, string? currentFilter, string? searchString, int? pageIndex)
        {
            SortOrder = sortOrder;
            CurrentFilter = searchString ?? currentFilter ?? "";

            NameSort = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            PriceSort = sortOrder == "price_asc" ? "price_desc" : "price_asc";
            StockSort = sortOrder == "stock_asc" ? "stock_desc" : "stock_asc";

            if (searchString != null)
            {
                pageIndex = 1;
            }
            else
            {
                searchString = currentFilter;
            }

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

            IQueryable<Product> productsIQ = _context.Products.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                productsIQ = productsIQ.Where(p =>
                    p.ProductName.Contains(searchString) ||
                    p.SKU.Contains(searchString) ||
                    (p.LocationCode != null && p.LocationCode.Contains(searchString)));
            }

            productsIQ = sortOrder switch
            {
                "name_desc" => productsIQ.OrderByDescending(p => p.ProductName),
                "price_asc" => productsIQ.OrderBy(p => p.BasePrice),
                "price_desc" => productsIQ.OrderByDescending(p => p.BasePrice),
                "stock_asc" => productsIQ.OrderBy(p => p.StockQuantity),
                "stock_desc" => productsIQ.OrderByDescending(p => p.StockQuantity),
                _ => productsIQ.OrderBy(p => p.ProductName)
            };

            var projectedIQ = productsIQ.Select(p => new ProductRow
            {
                ProductID = p.ProductID,
                SKU = p.SKU,
                ProductName = p.ProductName,
                Description = p.Description,
                BasePrice = p.BasePrice,
                StockQuantity = p.StockQuantity,
                ReservedQuantity = reservedLookup.ContainsKey(p.ProductID) ? reservedLookup[p.ProductID] : 0,
                AvailableQuantity = p.StockQuantity - (reservedLookup.ContainsKey(p.ProductID) ? reservedLookup[p.ProductID] : 0),
                LocationCode = p.LocationCode
            });

            Products = await PaginatedList<ProductRow>.CreateAsync(projectedIQ, pageIndex ?? 1, 10);
        }
    }
}