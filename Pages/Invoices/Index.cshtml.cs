using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AH.Data;
using AHInteriorsERP.Models;

namespace AHInteriorsERP.Pages.Invoices
{
    public class IndexModel : PageModel
    {
        private readonly AHInteriorsERPContext _context;

        public IndexModel(AHInteriorsERPContext context)
        {
            _context = context;
        }

        public PaginatedList<Invoice> Invoice { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public string? SearchString { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SortOrder { get; set; }

        public string CurrentFilter { get; set; } = "";
        public string NumberSort { get; set; } = "";
        public string DateSort { get; set; } = "";
        public string TotalSort { get; set; } = "";

        public async Task OnGetAsync(string? sortOrder, string? currentFilter, string? searchString, int? pageIndex)
        {
            SortOrder = sortOrder;
            CurrentFilter = searchString ?? currentFilter ?? "";

            NumberSort = string.IsNullOrEmpty(sortOrder) ? "number_desc" : "";
            DateSort = sortOrder == "date_asc" ? "date_desc" : "date_asc";
            TotalSort = sortOrder == "total_asc" ? "total_desc" : "total_asc";

            if (searchString != null)
            {
                pageIndex = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            IQueryable<Invoice> invoicesIQ = _context.Invoices
                .AsNoTracking()
                .Include(i => i.Order)
                    .ThenInclude(o => o.Customer);

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                invoicesIQ = invoicesIQ.Where(i =>
                    i.InvoiceNumber.Contains(searchString) ||
                    i.OrderID.ToString().Contains(searchString) ||
                    (i.Order != null && i.Order.Customer != null && i.Order.Customer.CustomerName.Contains(searchString)));
            }

            invoicesIQ = sortOrder switch
            {
                "number_desc" => invoicesIQ.OrderByDescending(i => i.InvoiceNumber),
                "date_asc" => invoicesIQ.OrderBy(i => i.InvoiceDate),
                "date_desc" => invoicesIQ.OrderByDescending(i => i.InvoiceDate),
                "total_asc" => invoicesIQ.OrderBy(i => i.TotalAmount),
                "total_desc" => invoicesIQ.OrderByDescending(i => i.TotalAmount),
                _ => invoicesIQ.OrderBy(i => i.InvoiceNumber)
            };

            Invoice = await PaginatedList<Invoice>.CreateAsync(invoicesIQ, pageIndex ?? 1, 10);
        }
    }
}