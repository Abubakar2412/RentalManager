using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace RentalManager.Pages.Client;
[Authorize(Policy="ClientSession")]
public class LogoutModel:PageModel{public async Task<IActionResult> OnPostAsync(){await HttpContext.SignOutAsync();return RedirectToPage("Login");}}
