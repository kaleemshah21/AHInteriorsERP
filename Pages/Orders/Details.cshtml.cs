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

namespace AHInteriorsERP.Pages.Orders
{
    [Authorize(Roles = "Admin,Staff,Warehouse")]
    public class DetailsModel : PageModel
    {

        private readonly AH.Data.AHInteriorsERPContext _context;

        public DetailsModel(AH.Data.AHInteriorsERPContext context)
        {
            _context = context;
        }

        public Order Order { get; set; } = default!;
        public decimal OrderTotal { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Load the Order + Customer + OrderItems + Product details
            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.OrderID == id.Value);

            if (order == null)
            {
                return NotFound();
            }
            else
            {
                Order = order;
            }
            OrderTotal = Order.OrderItems.Sum(oi => oi.Quantity * oi.UnitPriceAtTime);
            return Page();
        }
    }
}
