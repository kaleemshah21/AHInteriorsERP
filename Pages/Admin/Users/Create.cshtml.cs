using AHInteriorsERP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

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
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; } = "";

        [Required(ErrorMessage = "Role is required")]
        public string Role { get; set; } = "";
    }

    public void OnGet()
    {
        Roles = _roleManager.Roles
            .Select(r => r.Name!)
            .ToList();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Repopulate roles in case of validation failure
        Roles = _roleManager.Roles
            .Select(r => r.Name!)
            .ToList();

        if (!ModelState.IsValid)
            return Page();

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
            ModelState.AddModelError(string.Empty, error.Description);

        
        return Page();
    }
}