using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RentalManager.Data;
namespace RentalManager.Pages.Account;
[Authorize]
public class PasswordModel(IDbContextFactory<RentalDbContext> factory) : PageModel {
 [BindProperty,Required,StringLength(200)] public string CurrentPassword {get;set;}="";
 [BindProperty,Required,StringLength(200,MinimumLength=12)] public string NewPassword {get;set;}="";
 [BindProperty,Required,Compare(nameof(NewPassword))] public string ConfirmPassword {get;set;}="";
 public async Task<IActionResult> OnPostAsync(){
  if(!ModelState.IsValid)return Page();await using var db=await factory.CreateDbContextAsync();var admin=await db.Admins.SingleAsync();
  var hasher=new PasswordHasher<string>();if(hasher.VerifyHashedPassword(admin.Username,admin.PasswordHash,CurrentPassword)==PasswordVerificationResult.Failed){ModelState.AddModelError("","Current password is incorrect.");return Page();}
  admin.PasswordHash=hasher.HashPassword(admin.Username,NewPassword);admin.SecurityStamp=Guid.NewGuid().ToString("N");await db.SaveChangesAsync();await HttpContext.SignOutAsync();return RedirectToPage("Login");
 }
}
