using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AH.Data;

public class IndexModel : PageModel
{
    private readonly AHInteriorsERPContext _context;

    public IndexModel(AHInteriorsERPContext context)
    {
        _context = context;
    }

    // KPI tiles
    public int OrdersToday { get; set; }
    public int LowStockCount { get; set; }
    public decimal TotalSales { get; set; }
    public int CustomerCount { get; set; }
    public int ProductCount { get; set; }

    // Dashboard lists
    public List<RecentOrderRow> RecentOrders { get; set; } = new();
    public List<LowStockRow> LowStockProducts { get; set; } = new();

    // Adjust this threshold whenever you want
    private const int LowStockThreshold = 5;

    public async Task OnGetAsync()
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);

        CustomerCount = await _context.Customers.AsNoTracking().CountAsync();
        ProductCount = await _context.Products.AsNoTracking().CountAsync();

        OrdersToday = await _context.Orders
            .AsNoTracking()
            .CountAsync(o => o.OrderDate >= today && o.OrderDate < tomorrow);

        LowStockCount = await _context.Products
            .AsNoTracking()
            .CountAsync(p => p.StockQuantity <= LowStockThreshold);

        var completeOrders = await _context.Orders
            .AsNoTracking()
            .Include(o => o.OrderItems)
            .ToListAsync();

        TotalSales = completeOrders
            .SelectMany(o => o.OrderItems)
            .Sum(oi => oi.Quantity * oi.UnitPriceAtTime);




        RecentOrders = await _context.Orders
            .AsNoTracking()
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
            .OrderByDescending(o => o.OrderDate)
            .Take(5)
            .Select(o => new RecentOrderRow
            {
                OrderID = o.OrderID,
                CustomerName = o.Customer != null ? o.Customer.CustomerName : "",
                Status = o.Status,
                Total = o.OrderItems.Sum(oi => oi.Quantity * oi.UnitPriceAtTime)
            })
            .ToListAsync();

        LowStockProducts = await _context.Products
            .AsNoTracking()
            .Where(p => p.StockQuantity <= LowStockThreshold)
            .OrderBy(p => p.StockQuantity)
            .Take(5)
            .Select(p => new LowStockRow
            {
                ProductID = p.ProductID,
                SKU = p.SKU,
                ProductName = p.ProductName,
                StockQuantity = p.StockQuantity
            })
            .ToListAsync();
    }

    public class RecentOrderRow
    {
        public int OrderID { get; set; }
        public string? CustomerName { get; set; }
        public string? Status { get; set; }
        public decimal Total { get; set; }
    }

    public class LowStockRow
    {
        public int ProductID { get; set; }
        public string SKU { get; set; } = "";
        public string ProductName { get; set; } = "";
        public int StockQuantity { get; set; }
    }
}
