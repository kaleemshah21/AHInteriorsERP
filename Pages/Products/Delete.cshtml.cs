using AH.Data;
using AHInteriorsERP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
// This file loads the selected product and disables it instead of deleting it,
// so previous orders and invoices remain accurate.
namespace AHInteriorsERP.Pages.Products
{
    [Authorize(Roles = "Admin,Staff,Warehouse")]
    public class DeleteModel : PageModel
    {
        private readonly AH.Data.AHInteriorsERPContext _context;

        public DeleteModel(AH.Data.AHInteriorsERPContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Product Product { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products.FirstOrDefaultAsync(m => m.ProductID == id);

            if (product == null)
            {
                return NotFound();
            }
            else
            {
                Product = product;
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                Product = product;
                //rather than deleting the product, it sets it as inactive so that any orders with the disabled products are still correct
                product.isActive = false;
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
