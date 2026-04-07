using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AH.Data;
using AHInteriorsERP.Models;

namespace AHInteriorsERP.Pages.Invoices
{
    public class DetailsModel : PageModel
    {
        private readonly AH.Data.AHInteriorsERPContext _context;

        public DetailsModel(AH.Data.AHInteriorsERPContext context)
        {
            _context = context;
        }

        public Invoice Invoice { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var invoice = await _context.Invoices
            .Include(i => i.Order)
                .ThenInclude(o => o.Customer)
            .Include(i => i.InvoiceItems)
            .FirstOrDefaultAsync(i => i.InvoiceID == id);

            if (invoice == null)
            {
                return NotFound();
            }

            Invoice = invoice;
            return Page();
        }
    }
}
