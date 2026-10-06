using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.EntityFrameworkCore;
using RentalManager.Data;
namespace RentalManager.Services;
// Periodically revalidate long-running server circuits against the administrator table.
public sealed class RentalAuthenticationStateProvider(ILoggerFactory loggerFactory,IDbContextFactory<RentalDbContext> factory) : RevalidatingServerAuthenticationStateProvider(loggerFactory) {
 protected override TimeSpan RevalidationInterval=>TimeSpan.FromMinutes(5);
 protected override async Task<bool> ValidateAuthenticationStateAsync(AuthenticationState state,CancellationToken cancellationToken) {
  var name=state.User.Identity?.Name;if(name is null)return false;
  await using var db=await factory.CreateDbContextAsync(cancellationToken);
  var stamp=state.User.FindFirst("securityStamp")?.Value;
  var expires=state.User.FindFirst("sessionExpires")?.Value;
  if(!long.TryParse(expires,out var unix)||DateTimeOffset.UtcNow.ToUnixTimeSeconds()>=unix)return false;
  return await db.Admins.AnyAsync(x=>x.Id==1&&x.Username==name&&x.SecurityStamp==stamp,cancellationToken);
 }
}
