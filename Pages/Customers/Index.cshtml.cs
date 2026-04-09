using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AH.Data;
using AHInteriorsERP.Models;

namespace AHInteriorsERP.Pages.Customers
{
    public class IndexModel : PageModel
    {
        private readonly AHInteriorsERPContext _context;

        public IndexModel(AHInteriorsERPContext context)
        {
            _context = context;
        }

        public PaginatedList<Customer> Customer { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public string? SearchString { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SortOrder { get; set; }

        public string CurrentFilter { get; set; } = "";
        public string NameSort { get; set; } = "";
        public string CitySort { get; set; } = "";
        public string CreatedSort { get; set; } = "";

        public async Task OnGetAsync(string? sortOrder, string? currentFilter, string? searchString, int? pageIndex)
        {
            SortOrder = sortOrder;
            CurrentFilter = searchString ?? currentFilter ?? "";

            NameSort = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            CitySort = sortOrder == "city_asc" ? "city_desc" : "city_asc";
            CreatedSort = sortOrder == "date_asc" ? "date_desc" : "date_asc";

            if (searchString != null)
            {
                pageIndex = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            IQueryable<Customer> customerIQ = _context.Customers.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                customerIQ = customerIQ.Where(c =>
                    c.CustomerName.Contains(searchString) ||
                    (c.Email != null && c.Email.Contains(searchString)) ||
                    (c.Phone != null && c.Phone.Contains(searchString)) ||
                    (c.City != null && c.City.Contains(searchString)));
            }

            customerIQ = sortOrder switch
            {
                "name_desc" => customerIQ.OrderByDescending(c => c.CustomerName),
                "city_asc" => customerIQ.OrderBy(c => c.City),
                "city_desc" => customerIQ.OrderByDescending(c => c.City),
                "date_asc" => customerIQ.OrderBy(c => c.CreatedAt),
                "date_desc" => customerIQ.OrderByDescending(c => c.CreatedAt),
                _ => customerIQ.OrderBy(c => c.CustomerName)
            };

            Customer = await PaginatedList<Customer>.CreateAsync(customerIQ, pageIndex ?? 1, 10);
        }
    }
}