using AHInteriorsERP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AHInteriorsERP.Pages.Admin.Users;

[Authorize(Roles = "Admin")]


public class CreateModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;

    private readonly RoleManager<IdentityRole> _roleManager;


    public CreateModel(UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public List<string> Roles { get; set; } = new();

    [BindProperty]
    public InputModel Input { get; set; } = new();


    public class InputModel
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public string Role { get; set; } = "";
    }


    public void OnGet() {
        Roles = _roleManager.Roles
        .Select(r => r.Name!)
        .ToList();

    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = new ApplicationUser
        {
            UserName = Input.Email,
            Email = Input.Email
        };

        var result = await _userManager.CreateAsync(user, Input.Password);
        if (result.Succeeded)
        {
            if (!string.IsNullOrEmpty(Input.Role))
                await _userManager.AddToRoleAsync(user, Input.Role);

            return RedirectToPage("./Index");
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError("", error.Description);

        return Page();
    }
}
