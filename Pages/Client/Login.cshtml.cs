using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using RentalManager.Services;
namespace RentalManager.Pages.Client;
[EnableRateLimiting("login")]
public class LoginModel(IClientAccountService accounts):PageModel {
 [BindProperty,Required,StringLength(80)]public string Username{get;set;}="";
 [BindProperty,Required,StringLength(200)]public string Password{get;set;}="";
 public IActionResult OnGet()=>User.IsInRole("Client")?LocalRedirect(User.FindFirst("passwordChangeRequired")?.Value=="true"?"/Client/Password":"/client"):Page();
 public async Task<IActionResult> OnPostAsync(){if(!ModelState.IsValid)return Page();var client=await accounts.VerifyAsync(Username,Password);if(client is null){ModelState.AddModelError("","Invalid username or password, or account disabled.");return Page();}var expires=DateTimeOffset.UtcNow.AddHours(8);var claims=new[]{new Claim(ClaimTypes.Name,client.Username),new Claim(ClaimTypes.Role,"Client"),new Claim("clientId",client.AccountId.ToString()),new Claim("tenantId",client.TenantId.ToString()),new Claim("securityStamp",client.Stamp),new Claim("passwordChangeRequired",client.MustChangePassword?"true":"false"),new Claim("sessionExpires",expires.ToUnixTimeSeconds().ToString())};await HttpContext.SignInAsync(new ClaimsPrincipal(new ClaimsIdentity(claims,CookieAuthenticationDefaults.AuthenticationScheme)),new AuthenticationProperties{IsPersistent=false,ExpiresUtc=expires});return LocalRedirect(client.MustChangePassword?"/Client/Password":"/client");}
}
