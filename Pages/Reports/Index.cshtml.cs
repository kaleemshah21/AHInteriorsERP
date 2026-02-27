using System.Text;
using AH.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
namespace AHInteriorsERP.Pages.Reports;

public class IndexModel : PageModel
{
    private readonly AHInteriorsERPContext _context;

    public IndexModel(AHInteriorsERPContext context)
    {
        _context = context;
    }

    [BindProperty(SupportsGet = true)]
    public DateTime? From { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? To { get; set; }

    public decimal TotalSales { get; set; }
    public int OrdersCount { get; set; }

    public List<OrderRow> Orders { get; set; } = new();
    public List<ProductSalesRow> BestSellers { get; set; } = new();
    public List<ProductSalesRow> WorstSellers { get; set; } = new();

    public class ProductSalesRow
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; } = "";
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
    }

    public class OrderRow
    {
        public int OrderID { get; set; }
        public DateTime OrderDate { get; set; }
        public string CustomerName { get; set; } = "";
        public string Status { get; set; } = "";
        public decimal Total { get; set; }
    }

    private (DateTime start, DateTime endExclusive) GetRange()
    {
        var start = (From?.Date) ?? DateTime.UtcNow.Date.AddDays(-30);
        var endExclusive = ((To?.Date) ?? DateTime.UtcNow.Date).AddDays(1);
        return (start, endExclusive);
    }

    public async Task OnGetAsync()
    {
        var (start, endExclusive) = GetRange();

        // Product Sales
        var productSales = await _context.OrderItems
            .AsNoTracking()
            .Where(oi =>
                oi.Order != null &&
                oi.Order.OrderDate >= start &&
                oi.Order.OrderDate < endExclusive)
            .GroupBy(oi => new
            {
                oi.ProductID,
                oi.Product!.ProductName
            })
            .Select(g => new ProductSalesRow
            {
                ProductID = g.Key.ProductID,
                ProductName = g.Key.ProductName,
                QuantitySold = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x => x.Quantity * x.UnitPriceAtTime)
            })
            .ToListAsync();

        BestSellers = productSales
            .OrderByDescending(p => p.QuantitySold)
            .Take(5)
            .ToList();

        WorstSellers = productSales
            .OrderBy(p => p.QuantitySold)
            .Take(5)
            .ToList();

        // Orders
        Orders = await _context.Orders
            .AsNoTracking()
            .Where(o => o.OrderDate >= start && o.OrderDate < endExclusive)
            .OrderByDescending(o => o.OrderDate)
            .Select(o => new OrderRow
            {
                OrderID = o.OrderID,
                OrderDate = o.OrderDate,
                CustomerName = o.Customer != null ? o.Customer.CustomerName : "",
                Status = o.Status,
                Total = o.OrderItems.Sum(oi => oi.Quantity * oi.UnitPriceAtTime)
            })
            .ToListAsync();

        OrdersCount = Orders.Count;
        TotalSales = Orders.Sum(o => o.Total);
    }

    // EXPORT ORDERS

    public async Task<IActionResult> OnGetExportOrdersPdfAsync()
    {
        var (start, endExclusive) = GetRange();

        var rows = await _context.Orders
            .AsNoTracking()
            .Where(o => o.OrderDate >= start && o.OrderDate < endExclusive)
            .OrderByDescending(o => o.OrderDate)
            .Select(o => new OrderRow
            {
                OrderID = o.OrderID,
                OrderDate = o.OrderDate,
                CustomerName = o.Customer != null ? o.Customer.CustomerName : "",
                Status = o.Status,
                Total = o.OrderItems.Sum(oi => oi.Quantity * oi.UnitPriceAtTime)
            })
            .ToListAsync();

        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);

                page.Header().Text("Orders Report").FontSize(20).SemiBold();

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(70);
                        columns.ConstantColumn(90);
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.ConstantColumn(80);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Text("Order #").SemiBold();
                        header.Cell().Text("Date").SemiBold();
                        header.Cell().Text("Customer").SemiBold();
                        header.Cell().Text("Status").SemiBold();
                        header.Cell().AlignRight().Text("Total").SemiBold();
                    });

                    foreach (var r in rows)
                    {
                        table.Cell().Text(r.OrderID.ToString());
                        table.Cell().Text(r.OrderDate.ToString("yyyy-MM-dd"));
                        table.Cell().Text(r.CustomerName);
                        table.Cell().Text(r.Status);
                        table.Cell().AlignRight().Text($"£{r.Total:0.00}");
                    }
                });
            });
        });

        return File(document.GeneratePdf(), "application/pdf", "OrdersReport.pdf");
    }

    public async Task<IActionResult> OnGetExportBestPdfAsync()
    {
        var (start, endExclusive) = GetRange();

        var rows = await _context.OrderItems
            .AsNoTracking()
            .Where(oi => oi.Order.OrderDate >= start && oi.Order.OrderDate < endExclusive)
            .GroupBy(oi => new { oi.ProductID, oi.Product.ProductName })
            .Select(g => new ProductSalesRow
            {
                ProductID = g.Key.ProductID,
                ProductName = g.Key.ProductName,
                QuantitySold = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x => x.Quantity * x.UnitPriceAtTime)
            })
            .OrderByDescending(p => p.QuantitySold)
            .Take(5)
            .ToListAsync();

        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Header().Text("Best Sellers Report").FontSize(20).SemiBold();

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.ConstantColumn(80);
                        columns.ConstantColumn(100);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Text("Product").SemiBold();
                        header.Cell().AlignRight().Text("Qty").SemiBold();
                        header.Cell().AlignRight().Text("Revenue").SemiBold();
                    });

                    foreach (var r in rows)
                    {
                        table.Cell().Text(r.ProductName);
                        table.Cell().AlignRight().Text(r.QuantitySold.ToString());
                        table.Cell().AlignRight().Text($"£{r.Revenue:0.00}");
                    }
                });
            });
        });

        return File(document.GeneratePdf(), "application/pdf", "BestSellers.pdf");
    }

    public async Task<IActionResult> OnGetExportWorstPdfAsync()
    {
        var (start, endExclusive) = GetRange();

        var rows = await _context.OrderItems
            .AsNoTracking()
            .Where(oi => oi.Order.OrderDate >= start && oi.Order.OrderDate < endExclusive)
            .GroupBy(oi => new { oi.ProductID, oi.Product.ProductName })
            .Select(g => new ProductSalesRow
            {
                ProductID = g.Key.ProductID,
                ProductName = g.Key.ProductName,
                QuantitySold = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x => x.Quantity * x.UnitPriceAtTime)
            })
            .OrderBy(p => p.QuantitySold)
            .Take(5)
            .ToListAsync();

        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Header().Text("Worst Sellers Report").FontSize(20).SemiBold();

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.ConstantColumn(80);
                        columns.ConstantColumn(100);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Text("Product").SemiBold();
                        header.Cell().AlignRight().Text("Qty").SemiBold();
                        header.Cell().AlignRight().Text("Revenue").SemiBold();
                    });

                    foreach (var r in rows)
                    {
                        table.Cell().Text(r.ProductName);
                        table.Cell().AlignRight().Text(r.QuantitySold.ToString());
                        table.Cell().AlignRight().Text($"£{r.Revenue:0.00}");
                    }
                });
            });
        });

        return File(document.GeneratePdf(), "application/pdf", "WorstSellers.pdf");
    }
    public async Task<IActionResult> OnGetExportOrdersAsync()
    {
        var (start, endExclusive) = GetRange();

        var rows = await _context.Orders
            .AsNoTracking()
            .Where(o => o.OrderDate >= start && o.OrderDate < endExclusive)
            .OrderByDescending(o => o.OrderDate)
            .Select(o => new OrderRow
            {
                OrderID = o.OrderID,
                OrderDate = o.OrderDate,
                CustomerName = o.Customer != null ? o.Customer.CustomerName : "",
                Status = o.Status,
                Total = o.OrderItems.Sum(oi => oi.Quantity * oi.UnitPriceAtTime)
            })
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("OrderID,OrderDate,Customer,Status,Total");

        foreach (var r in rows)
        {
            string esc(string s) => "\"" + (s ?? "").Replace("\"", "\"\"") + "\"";
            sb.AppendLine($"{r.OrderID},{r.OrderDate:yyyy-MM-dd},{esc(r.CustomerName)},{esc(r.Status)},{r.Total:0.00}");
        }

        return File(Encoding.UTF8.GetBytes(sb.ToString()),
            "text/csv",
            "OrdersReport.csv");
    }

    // EXPORT BEST SELLERS
    public async Task<IActionResult> OnGetExportBestAsync()
    {
        var (start, endExclusive) = GetRange();

        var rows = await _context.OrderItems
            .AsNoTracking()
            .Where(oi => oi.Order.OrderDate >= start && oi.Order.OrderDate < endExclusive)
            .GroupBy(oi => new { oi.ProductID, oi.Product.ProductName })
            .Select(g => new ProductSalesRow
            {
                ProductID = g.Key.ProductID,
                ProductName = g.Key.ProductName,
                QuantitySold = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x => x.Quantity * x.UnitPriceAtTime)
            })
            .OrderByDescending(p => p.QuantitySold)
            .Take(5)
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Product,QuantitySold,Revenue");

        foreach (var r in rows)
        {
            string esc(string s) => "\"" + (s ?? "").Replace("\"", "\"\"") + "\"";
            sb.AppendLine($"{esc(r.ProductName)},{r.QuantitySold},{r.Revenue:0.00}");
        }

        return File(Encoding.UTF8.GetBytes(sb.ToString()),
            "text/csv",
            "BestSellers.csv");
    }

    // EXPORT WORST SELLERS
    public async Task<IActionResult> OnGetExportWorstAsync()
    {
        var (start, endExclusive) = GetRange();

        var rows = await _context.OrderItems
            .AsNoTracking()
            .Where(oi => oi.Order.OrderDate >= start && oi.Order.OrderDate < endExclusive)
            .GroupBy(oi => new { oi.ProductID, oi.Product.ProductName })
            .Select(g => new ProductSalesRow
            {
                ProductID = g.Key.ProductID,
                ProductName = g.Key.ProductName,
                QuantitySold = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x => x.Quantity * x.UnitPriceAtTime)
            })
            .OrderBy(p => p.QuantitySold)
            .Take(5)
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Product,QuantitySold,Revenue");

        foreach (var r in rows)
        {
            string esc(string s) => "\"" + (s ?? "").Replace("\"", "\"\"") + "\"";
            sb.AppendLine($"{esc(r.ProductName)},{r.QuantitySold},{r.Revenue:0.00}");
        }

        return File(Encoding.UTF8.GetBytes(sb.ToString()),
            "text/csv",
            "WorstSellers.csv");
    }
}