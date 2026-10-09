using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
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
 o.AccessDeniedPath="/Account/Denied";
 o.Events.OnRedirectToLogin=context=>{if(context.Request.Path.StartsWithSegments("/client"))context.Response.Redirect("/Client/Login");else context.Response.Redirect(context.RedirectUri);return Task.CompletedTask;};
 o.Events.OnValidatePrincipal=async context=>{
  var user=context.Principal;var name=user?.Identity?.Name;var stamp=user?.FindFirst("securityStamp")?.Value;bool valid=false;
  if(user?.IsInRole("Client")==true){if(name is not null&&int.TryParse(user.FindFirst("clientId")?.Value,out var id)&&int.TryParse(user.FindFirst("tenantId")?.Value,out var tenant))valid=await context.HttpContext.RequestServices.GetRequiredService<IClientAccountService>().ValidateAsync(id,tenant,name,stamp);}
  else if(name is not null){valid=await context.HttpContext.RequestServices.GetRequiredService<IAdminAccountService>().ValidateAsync(name,stamp);if(valid&&!user!.IsInRole("Admin")&&user.Identity is ClaimsIdentity identity){identity.AddClaim(new(ClaimTypes.Role,"Admin"));context.ShouldRenew=true;}}
  if(!valid){context.RejectPrincipal();await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);}
 };
});
builder.Services.AddAuthorization(o=>{o.DefaultPolicy=new AuthorizationPolicyBuilder().RequireAuthenticatedUser().RequireRole("Admin").Build();o.AddPolicy("ClientSession",p=>p.RequireAuthenticatedUser().RequireRole("Client"));o.AddPolicy("Client",p=>p.RequireAuthenticatedUser().RequireRole("Client").RequireClaim("passwordChangeRequired","false"));});builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider,RentalAuthenticationStateProvider>();
builder.Services.AddScoped<ICurrentUser,BlazorCurrentUser>();
builder.Services.AddScoped<IContractRenderer,HtmlContractRenderer>();
builder.Services.AddScoped<IRentalRepository,EfRentalRepository>();
builder.Services.AddScoped<IAdminAccountService,AdminAccountService>();
builder.Services.AddScoped<RentalService>();
builder.Services.AddScoped<IWorkspaceRepository,EfWorkspaceRepository>();
builder.Services.AddScoped<WorkspaceService>();
builder.Services.AddScoped<IClientAccountService,ClientAccountService>();
builder.Services.AddScoped<IClientPortalRepository,EfClientPortalRepository>();
builder.Services.AddScoped<ICurrentClient,BlazorCurrentClient>();
builder.Services.AddScoped<ClientPortalService>();
builder.Services.AddScoped<ClientAdminService>();
builder.Services.AddSingleton(TimeProvider.System);
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
app.MapGet("/client/documents/contracts/{id:int}",async(int id,ClaimsPrincipal user,IClientPortalRepository repository)=>{var identity=ClientClaims.Require(user);var html=await repository.ContractAsync(id,identity.TenantId);return html is null?Results.NotFound():Results.Content(html,"text/html");}).RequireAuthorization("Client");
app.Run();
public partial class Program { }
