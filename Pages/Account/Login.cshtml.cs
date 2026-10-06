using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using RentalManager.Data;
namespace RentalManager.Pages.Account;
[EnableRateLimiting("login")]
public class LoginModel(IDbContextFactory<RentalDbContext> factory) : PageModel {
 [BindProperty,Required,StringLength(80)] public string Username {get;set;}="";
 [BindProperty,Required,StringLength(200)] public string Password {get;set;}="";
 public async Task<IActionResult> OnGetAsync(){await using var db=await factory.CreateDbContextAsync();if(!await db.Admins.AnyAsync())return RedirectToPage("Setup");if(User.Identity?.IsAuthenticated==true)return LocalRedirect("/");return Page();}
 public async Task<IActionResult> OnPostAsync(){
  if(!ModelState.IsValid)return Page();await using var db=await factory.CreateDbContextAsync();var admin=await db.Admins.SingleOrDefaultAsync();
  if(admin is null)return RedirectToPage("Setup");
  if(Username.Trim()!=admin.Username||new PasswordHasher<string>().VerifyHashedPassword(admin.Username,admin.PasswordHash,Password)==PasswordVerificationResult.Failed){ModelState.AddModelError("","Invalid username or password.");return Page();}
  var expires=DateTimeOffset.UtcNow.AddHours(8);
  var principal=new ClaimsPrincipal(new ClaimsIdentity(new[]{new Claim(ClaimTypes.Name,admin.Username),new Claim("securityStamp",admin.SecurityStamp),new Claim("sessionExpires",expires.ToUnixTimeSeconds().ToString())},CookieAuthenticationDefaults.AuthenticationScheme));
  await HttpContext.SignInAsync(principal,new AuthenticationProperties{IsPersistent=false,ExpiresUtc=expires});
  return LocalRedirect("/");
 }
}
