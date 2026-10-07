using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;

using RentalManager.Services;
namespace RentalManager.Pages.Account;
[EnableRateLimiting("login")]
public class SetupModel(IAdminAccountService accounts) : PageModel {
 [BindProperty,Required,StringLength(80,MinimumLength=3)] public string Username {get;set;}="";
 [BindProperty,Required,StringLength(200,MinimumLength=12)] public string Password {get;set;}="";
 [BindProperty,Required,Compare(nameof(Password))] public string ConfirmPassword {get;set;}="";
 public async Task<IActionResult> OnGetAsync(){return await accounts.ExistsAsync()?RedirectToPage("Login"):Page();}
 public async Task<IActionResult> OnPostAsync(){
  if(!ModelState.IsValid)return Page();if(await accounts.ExistsAsync())return RedirectToPage("Login");
  var name=Username.Trim();if(name.Length<3){ModelState.AddModelError("","Username must contain at least 3 characters.");return Page();}
  if(!await accounts.CreateAsync(name,Password)){ModelState.AddModelError("","Administrator already created or database unavailable. Return to sign in.");return Page();}
  return RedirectToPage("Login");
 }
}
