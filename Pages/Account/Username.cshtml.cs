using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using RentalManager.Services;
namespace RentalManager.Pages.Account;
[Authorize,EnableRateLimiting("login")]
public class UsernameModel(IAdminAccountService accounts):PageModel {
 [BindProperty,Required,StringLength(80,MinimumLength=3)]public string NewUsername{get;set;}="";
 [BindProperty,Required,StringLength(200)]public string CurrentPassword{get;set;}="";
 public void OnGet()=>NewUsername=User.Identity?.Name??"";
 public async Task<IActionResult> OnPostAsync(){if(!ModelState.IsValid)return Page();try{if(!await accounts.RenameAsync(User.Identity?.Name??"",CurrentPassword,NewUsername)){ModelState.AddModelError("","Current password is incorrect.");return Page();}}catch(InvalidOperationException e){ModelState.AddModelError("",e.Message);return Page();}await HttpContext.SignOutAsync();return RedirectToPage("Login");}
}
