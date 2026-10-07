using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;


namespace RentalManager.Services;
// Periodically revalidate long-running server circuits against the administrator table.
public sealed class RentalAuthenticationStateProvider(ILoggerFactory loggerFactory,IAdminAccountService accounts) : RevalidatingServerAuthenticationStateProvider(loggerFactory) {
 protected override TimeSpan RevalidationInterval=>TimeSpan.FromMinutes(5);
 protected override async Task<bool> ValidateAuthenticationStateAsync(AuthenticationState state,CancellationToken cancellationToken) {
  var name=state.User.Identity?.Name;if(name is null)return false;
  var stamp=state.User.FindFirst("securityStamp")?.Value;
  var expires=state.User.FindFirst("sessionExpires")?.Value;
  if(!long.TryParse(expires,out var unix)||DateTimeOffset.UtcNow.ToUnixTimeSeconds()>=unix)return false;
  return await accounts.ValidateAsync(name,stamp,cancellationToken);
 }
}
