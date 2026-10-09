using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using RentalManager.Services;
namespace RentalManager.Pages.Client;
[Authorize(Policy="ClientSession"),EnableRateLimiting("login")]
public class PasswordModel(IClientAccountService accounts):PageModel {
 [BindProperty,Required,StringLength(200)]public string CurrentPassword{get;set;}="";
 [BindProperty,Required,StringLength(200,MinimumLength=12)]public string NewPassword{get;set;}="";
 [BindProperty,Required,Compare(nameof(NewPassword))]public string ConfirmPassword{get;set;}="";
 public async Task<IActionResult> OnPostAsync(){if(!ModelState.IsValid)return Page();if(!int.TryParse(User.FindFirst("clientId")?.Value,out var id)||!await accounts.ChangePasswordAsync(id,CurrentPassword,NewPassword)){ModelState.AddModelError("","Current password is incorrect.");return Page();}await HttpContext.SignOutAsync();return RedirectToPage("Login");}
}
