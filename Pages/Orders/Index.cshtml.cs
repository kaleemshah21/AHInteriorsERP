using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AH.Data;
using AHInteriorsERP.Models;

namespace AHInteriorsERP.Pages.Orders
{
    public class IndexModel : PageModel
    {
        private readonly AHInteriorsERPContext _context;

        public IndexModel(AHInteriorsERPContext context)
        {
            _context = context;
        }

        public PaginatedList<Order> Order { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public string? SearchString { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SortOrder { get; set; }

        public string CurrentFilter { get; set; } = "";
        public string DateSort { get; set; } = "";
        public string CustomerSort { get; set; } = "";
        public string StatusSort { get; set; } = "";

        public async Task OnGetAsync(string? sortOrder, string? currentFilter, string? searchString, int? pageIndex)
        {
            SortOrder = sortOrder;
            CurrentFilter = searchString ?? currentFilter ?? "";
            SearchString = CurrentFilter;

            DateSort = string.IsNullOrEmpty(sortOrder) ? "date_desc" : "";
            CustomerSort = sortOrder == "customer_asc" ? "customer_desc" : "customer_asc";
            StatusSort = sortOrder == "status_asc" ? "status_desc" : "status_asc";

            if (searchString != null)
            {
                pageIndex = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            IQueryable<Order> ordersIQ = _context.Orders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.OrderItems);

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var trimmedSearch = searchString.Trim();

                var statusMatched = Enum.TryParse<OrderStatus>(trimmedSearch, true, out var parsedStatus);

                ordersIQ = ordersIQ.Where(o =>
                    (o.Customer != null && o.Customer.CustomerName.Contains(trimmedSearch)) ||
                    (o.Notes != null && o.Notes.Contains(trimmedSearch)) ||
                    (statusMatched && o.Status == parsedStatus)
                );
            }

            ordersIQ = sortOrder switch
            {
                "date_desc" => ordersIQ.OrderByDescending(o => o.OrderDate),
                "customer_asc" => ordersIQ.OrderBy(o => o.Customer != null ? o.Customer.CustomerName : ""),
                "customer_desc" => ordersIQ.OrderByDescending(o => o.Customer != null ? o.Customer.CustomerName : ""),
                "status_asc" => ordersIQ.OrderBy(o => o.Status),
                "status_desc" => ordersIQ.OrderByDescending(o => o.Status),
                _ => ordersIQ.OrderBy(o => o.OrderDate)
            };

            Order = await PaginatedList<Order>.CreateAsync(ordersIQ, pageIndex ?? 1, 10);
        }
    }
}