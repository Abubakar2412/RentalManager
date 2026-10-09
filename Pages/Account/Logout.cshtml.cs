using Microsoft.AspNetCore.Authentication;
using RentalManager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace RentalManager.Pages.Account;
[Authorize]
public class LogoutModel(IAdminAccountService accounts) : PageModel {
 public async Task<IActionResult> OnPostAsync(){try{await accounts.RecordSignOutAsync(User.Identity?.Name??"");}finally{await HttpContext.SignOutAsync();}return RedirectToPage("Login");}
}
