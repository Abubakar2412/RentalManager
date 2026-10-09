using Microsoft.AspNetCore.Authentication;
using RentalManager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace RentalManager.Pages.Account;
[Authorize]
public class LogoutModel(IAdminAccountService accounts,ILogger<LogoutModel> logger) : PageModel {
 public async Task<IActionResult> OnPostAsync(){try{await accounts.RecordSignOutAsync(User.Identity?.Name??"");}catch(Exception e){logger.LogError(e,"Unable to record administrator sign-out.");}await HttpContext.SignOutAsync();return RedirectToPage("Login");}
}
