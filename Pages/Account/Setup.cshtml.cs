using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using RentalManager.Data;
namespace RentalManager.Pages.Account;
[EnableRateLimiting("login")]
public class SetupModel(IDbContextFactory<RentalDbContext> factory) : PageModel {
 [BindProperty,Required,StringLength(80,MinimumLength=3)] public string Username {get;set;}="";
 [BindProperty,Required,StringLength(200,MinimumLength=12)] public string Password {get;set;}="";
 [BindProperty,Required,Compare(nameof(Password))] public string ConfirmPassword {get;set;}="";
 public async Task<IActionResult> OnGetAsync(){await using var db=await factory.CreateDbContextAsync();return await db.Admins.AnyAsync()?RedirectToPage("Login"):Page();}
 public async Task<IActionResult> OnPostAsync(){
  if(!ModelState.IsValid)return Page();await using var db=await factory.CreateDbContextAsync();if(await db.Admins.AnyAsync())return RedirectToPage("Login");
  var name=Username.Trim();if(name.Length<3){ModelState.AddModelError("","Username must contain at least 3 characters.");return Page();}
  db.Admins.Add(new(){Id=1,Username=name,PasswordHash=new PasswordHasher<string>().HashPassword(name,Password)});
  try{await db.SaveChangesAsync();}catch(DbUpdateException){ModelState.AddModelError("","Administrator already created or database unavailable. Return to sign in.");return Page();}
  return RedirectToPage("Login");
 }
}
