using AHInteriorsERP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AHInteriorsERP.Pages.Admin.Users;

[Authorize(Roles = "Admin")]


public class DeleteModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;

    public DeleteModel(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public ApplicationUser User { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(string id)
    {
        User = await _userManager.FindByIdAsync(id);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);

        if (user != null)
            await _userManager.DeleteAsync(user);

        return RedirectToPage("./Index");
    }
}
