using AHInteriorsERP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AHInteriorsERP.Pages.Admin.Users;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public List<ApplicationUser> Users { get; set; } = new();

    // Dictionary to hold user roles for display
    public Dictionary<string, IList<string>> UserRoles { get; set; } = new();

    [TempData]
    public string StatusMessage { get; set; }

    public async Task OnGetAsync()
    {
        // Get all users
        Users = _userManager.Users.OrderBy(u => u.Email).ToList();

        // Fetch roles for each user
        UserRoles = new Dictionary<string, IList<string>>();
        foreach (var user in Users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            UserRoles[user.Id] = roles;
        }
    }

    // POST handler to reset 2FA
    public async Task<IActionResult> OnPostReset2FAAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            StatusMessage = "User not found.";
            return RedirectToPage();
        }

        await _userManager.SetTwoFactorEnabledAsync(user, false);
        await _userManager.ResetAuthenticatorKeyAsync(user);

        var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);

        StatusMessage = $"2FA has been reset for {user.Email}. They must set up their authenticator app again on next login.";

        return RedirectToPage();
    }
}