using System;
using System.Collections.Generic;
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
        private readonly AH.Data.AHInteriorsERPContext _context;

        public IndexModel(AH.Data.AHInteriorsERPContext context)
        {
            _context = context;
        }

        public IList<Customer> Customer { get; set; } = new List<Customer>();


        public async Task OnGetAsync()
        {
            Customer = await _context.Customers.AsNoTracking().ToListAsync();
        }
    }
}
