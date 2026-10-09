using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using RentalManager.Services;
using Xunit;
public class ClientPortalBoundaryTests {
 sealed class Identity:ICurrentClient{public Task<ClientIdentity> GetRequiredAsync()=>throw new UnauthorizedAccessException();}
 sealed class Clock(DateTimeOffset now):TimeProvider{public override DateTimeOffset GetUtcNow()=>now;}
 sealed class Repository:IClientPortalRepository {
  public int Calls;
  public Task<ClientDashboard> LoadAsync(int tenantId){Calls++;throw new Exception();}
  public Task<string?> ContractAsync(int id,int tenantId){Calls++;return Task.FromResult<string?>(null);}
  public Task SubmitAsync(RenewalDraft draft,int tenantId,string actor,DateOnly today){Calls++;return Task.CompletedTask;}
  public Task<IReadOnlyList<RenewalInfo>> RequestsAsync()=>Task.FromResult<IReadOnlyList<RenewalInfo>>([]);
  public Task ReviewAsync(int id,string response,string actor)=>Task.CompletedTask;
 }
 sealed class Authentication:AuthenticationStateProvider{public override Task<AuthenticationState> GetAuthenticationStateAsync()=>Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(new[]{new Claim(ClaimTypes.Name,"tenant"),new Claim(ClaimTypes.Role,"Client")},"test"))));}
 [Fact]public async Task ClientCannotUseAdministratorBoundary(){await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>new BlazorCurrentUser(new Authentication()).GetRequiredNameAsync());}
 [Fact]public async Task AnonymousClientCannotReadOrSubmit(){var repository=new Repository();var service=new ClientPortalService(repository,new Identity(),TimeProvider.System);await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>service.LoadAsync());await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>service.ContractAsync(1));await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>service.SubmitAsync(new(){LeaseId=1}));Assert.Equal(0,repository.Calls);}
 [Fact]public void ExpiryRemindersUseNoticePeriodAndEastAfricaDate(){var service=new ClientPortalService(new Repository(),new Identity(),new Clock(new(2026,10,9,22,0,0,TimeSpan.Zero)));Assert.Equal(new DateOnly(2026,10,10),service.Today);var data=new ClientDashboard("Client","TZS",[new(1,"Due",new(2026,1,1),new(2026,11,9),100,"Active",30,6),new(2,"Not due",new(2026,1,1),new(2026,11,10),100,"Active",30,6),new(3,"Cancelled",new(2026,1,1),new(2026,10,10),100,"Cancelled",30,6)],[],[]);Assert.Single(service.Reminders(data));Assert.Equal(30,service.Reminders(data)[0].DaysRemaining);}
}
