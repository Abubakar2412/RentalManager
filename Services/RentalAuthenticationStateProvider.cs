using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;


namespace RentalManager.Services;
// Periodically revalidate long-running server circuits against the administrator table.
public sealed class RentalAuthenticationStateProvider(ILoggerFactory loggerFactory,IAdminAccountService accounts,IClientAccountService clients) : RevalidatingServerAuthenticationStateProvider(loggerFactory) {
 protected override TimeSpan RevalidationInterval=>TimeSpan.FromMinutes(5);
 protected override async Task<bool> ValidateAuthenticationStateAsync(AuthenticationState state,CancellationToken cancellationToken) {
  var name=state.User.Identity?.Name;if(name is null)return false;
  var stamp=state.User.FindFirst("securityStamp")?.Value;
  var expires=state.User.FindFirst("sessionExpires")?.Value;
  if(!long.TryParse(expires,out var unix)||DateTimeOffset.UtcNow.ToUnixTimeSeconds()>=unix)return false;
  if(state.User.IsInRole("Client")){if(!int.TryParse(state.User.FindFirst("clientId")?.Value,out var id)||!int.TryParse(state.User.FindFirst("tenantId")?.Value,out var tenant))return false;return await clients.ValidateAsync(id,tenant,name,stamp,cancellationToken);}
  if(!state.User.IsInRole("Admin"))return false;
  return await accounts.ValidateAsync(name,stamp,cancellationToken);
 }
}
