using Microsoft.AspNetCore.Components.Authorization;
namespace RentalManager.Services;
public sealed class BlazorCurrentUser(AuthenticationStateProvider authentication):ICurrentUser {
 public async Task<string> GetRequiredNameAsync(){var user=(await authentication.GetAuthenticationStateAsync()).User;if(user.Identity?.IsAuthenticated!=true||!user.IsInRole("Admin"))throw new UnauthorizedAccessException("Please sign in again.");return user.Identity.Name??"administrator";}
}
public sealed class HtmlContractRenderer:IContractRenderer {
 public string Render(Lease lease,Property property,Tenant tenant,Landlord landlord)=>Contract.Render(lease,property,tenant,landlord);
}
