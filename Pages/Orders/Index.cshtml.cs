using System;
using System.Collections.Generic;
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
        private readonly AH.Data.AHInteriorsERPContext _context;

        public IndexModel(AH.Data.AHInteriorsERPContext context)
        {
            _context = context;
        }

        public IList<Order> Order { get;set; } = default!;

        public async Task OnGetAsync()
        {
            Order = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
            .ToListAsync();


        }
    }
}
