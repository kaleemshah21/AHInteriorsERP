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

    public void OnGet()
    {
        var today = DateTime.UtcNow.Date;

        CustomerCount = _context.Customers.Count();
        ProductCount = _context.Products.Count();

        OrdersToday = _context.Orders.Count(o => o.OrderDate >= today);

        LowStockCount = _context.Products.Count(p => p.StockQuantity <= LowStockThreshold);

        // TotalSales based on invoices (better for ERP/accounting)
        TotalSales = _context.Invoices.Any()
            ? _context.Invoices.Sum(i => i.TotalAmount)
            : 0m;

        // Recent orders (last 5) with calculated total from items
        RecentOrders = _context.Orders
            .AsNoTracking()
            .OrderByDescending(o => o.OrderDate)
            .Take(5)
            .Select(o => new RecentOrderRow
            {
                OrderID = o.OrderID,
                CustomerName = o.Customer != null ? o.Customer.CustomerName : "",
                Status = o.Status,
                Total = o.OrderItems.Sum(oi => oi.Quantity * oi.UnitPriceAtTime)
            })
            .ToList();

        // Low stock products (top 5)
        LowStockProducts = _context.Products
            .AsNoTracking()
            .Where(p => p.StockQuantity <= LowStockThreshold)
            .OrderBy(p => p.StockQuantity)
            .Take(5)
            .Select(p => new LowStockRow
            {
                SKU = p.SKU,
                ProductName = p.ProductName,
                StockQuantity = p.StockQuantity
            })
            .ToList();
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
        public string SKU { get; set; } = "";
        public string ProductName { get; set; } = "";
        public int StockQuantity { get; set; }
    }
}
