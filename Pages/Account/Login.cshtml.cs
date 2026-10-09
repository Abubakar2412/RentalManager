using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;

using RentalManager.Services;
namespace RentalManager.Pages.Account;
[EnableRateLimiting("login")]
public class LoginModel(IAdminAccountService accounts) : PageModel {
 [BindProperty,Required,StringLength(80)] public string Username {get;set;}="";
 [BindProperty,Required,StringLength(200)] public string Password {get;set;}="";
 public async Task<IActionResult> OnGetAsync(){if(!await accounts.ExistsAsync())return RedirectToPage("Setup");if(User.Identity?.IsAuthenticated==true)return LocalRedirect(User.IsInRole("Client")?"/client":"/");return Page();}
 public async Task<IActionResult> OnPostAsync(){
  if(!ModelState.IsValid)return Page();if(!await accounts.ExistsAsync())return RedirectToPage("Setup");
  var admin=await accounts.VerifyAsync(Username,Password);
  if(admin is null){ModelState.AddModelError("","Invalid username or password.");return Page();}
  var expires=DateTimeOffset.UtcNow.AddHours(8);
  var principal=new ClaimsPrincipal(new ClaimsIdentity(new[]{new Claim(ClaimTypes.Name,admin.Username),new Claim(ClaimTypes.Role,"Admin"),new Claim("securityStamp",admin.SecurityStamp),new Claim("sessionExpires",expires.ToUnixTimeSeconds().ToString())},CookieAuthenticationDefaults.AuthenticationScheme));
  await HttpContext.SignInAsync(principal,new AuthenticationProperties{IsPersistent=false,ExpiresUtc=expires});
  return LocalRedirect("/");
 }
}
