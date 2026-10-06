using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using RentalManager.Components;
using RentalManager.Data;
using RentalManager.Services;
var builder=WebApplication.CreateBuilder(args);
var connectionName=builder.Configuration["Database:ConnectionName"]??"DefaultConnectionOnHrPayMisHubDbDev";
var connection=builder.Configuration.GetConnectionString(connectionName)??throw new InvalidOperationException("Selected SQL Server connection string is missing.");
if(connection.Contains("REPLACE_VIA_SECRET_CONFIGURATION",StringComparison.Ordinal))throw new InvalidOperationException("Set the live connection string through secret configuration before starting in Production.");
builder.Services.AddDbContextFactory<RentalDbContext>(o=>o.UseSqlServer(connection));
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddRazorPages();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(o=>{
 o.LoginPath="/Account/Login";o.Cookie.Name="Chwaya.Rental.Session";o.Cookie.HttpOnly=true;o.Cookie.SameSite=SameSiteMode.Strict;o.Cookie.SecurePolicy=CookieSecurePolicy.SameAsRequest;o.ExpireTimeSpan=TimeSpan.FromHours(8);o.SlidingExpiration=false;
 o.Events.OnValidatePrincipal=async context=>{var user=context.Principal;var name=user?.Identity?.Name;var stamp=user?.FindFirst("securityStamp")?.Value;var factory=context.HttpContext.RequestServices.GetRequiredService<IDbContextFactory<RentalDbContext>>();await using var db=await factory.CreateDbContextAsync();if(name is null||!await db.Admins.AnyAsync(x=>x.Id==1&&x.Username==name&&x.SecurityStamp==stamp)){context.RejectPrincipal();await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);}};
});
builder.Services.AddAuthorization();builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider,RentalAuthenticationStateProvider>();
builder.Services.AddScoped<RentalService>();
builder.Services.AddRateLimiter(o=>o.AddPolicy("login",context=>RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString()??"local",_=>new FixedWindowRateLimiterOptions{PermitLimit=20,Window=TimeSpan.FromMinutes(1),QueueLimit=0})));
var app=builder.Build();
if(builder.Configuration.GetValue("Database:AutoMigrate",true)) {
 await using var scope=app.Services.CreateAsyncScope();
 var factory=scope.ServiceProvider.GetRequiredService<IDbContextFactory<RentalDbContext>>();
 await using var db=await factory.CreateDbContextAsync();
 await db.Database.MigrateAsync();
}
if(!app.Environment.IsDevelopment()){app.UseExceptionHandler("/Error");app.UseHsts();app.UseHttpsRedirection();}
app.Use(async(ctx,next)=>{ctx.Response.Headers["X-Content-Type-Options"]="nosniff";ctx.Response.Headers["X-Frame-Options"]="DENY";ctx.Response.Headers["Referrer-Policy"]="no-referrer";ctx.Response.Headers.CacheControl="no-store";await next();});
app.UseStaticFiles();app.UseRouting();app.UseRateLimiter();app.UseAuthentication();app.UseAuthorization();app.UseAntiforgery();
app.MapRazorPages();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.MapGet("/documents/contracts/{id:int}",async(int id,IDbContextFactory<RentalDbContext> factory)=>{
 await using var db=await factory.CreateDbContextAsync();var doc=await db.Contracts.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id);
 return doc is null?Results.NotFound():Results.Content(doc.Html,"text/html");
}).RequireAuthorization();
app.Run();
public partial class Program { }
