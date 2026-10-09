using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
namespace RentalManager.Services;
public static class ClientClaims {
 public static ClientIdentity Require(ClaimsPrincipal user){if(user.Identity?.IsAuthenticated!=true||!user.IsInRole("Client")||user.FindFirst("passwordChangeRequired")?.Value!="false"||!int.TryParse(user.FindFirst("clientId")?.Value,out var id)||!int.TryParse(user.FindFirst("tenantId")?.Value,out var tenant)||id<1||tenant<1)throw new UnauthorizedAccessException("Sign in to the client portal again.");return new(id,tenant,user.Identity.Name??"");}
}
public sealed class BlazorCurrentClient(AuthenticationStateProvider authentication):ICurrentClient {
 public async Task<ClientIdentity> GetRequiredAsync()=>ClientClaims.Require((await authentication.GetAuthenticationStateAsync()).User);
}
