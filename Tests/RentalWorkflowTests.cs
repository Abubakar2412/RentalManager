using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RentalManager;
using RentalManager.Data;
using RentalManager.Services;
using Xunit;
public class RentalWorkflowTests {
 sealed class Factory(DbContextOptions<RentalDbContext> options) : IDbContextFactory<RentalDbContext> {
  public RentalDbContext CreateDbContext()=>new(options);
  public Task<RentalDbContext> CreateDbContextAsync(CancellationToken cancellationToken=default)=>Task.FromResult(CreateDbContext());
 }
 sealed class ClientUser(ClientIdentity identity):ICurrentClient {public Task<ClientIdentity> GetRequiredAsync()=>Task.FromResult(identity);}
 sealed class FixedClock:TimeProvider {public override DateTimeOffset GetUtcNow()=>new(2026,12,20,0,0,0,TimeSpan.Zero);}
 sealed class CurrentUser : ICurrentUser {public Task<string> GetRequiredNameAsync()=>Task.FromResult("rental-admin");}
 sealed class Auth : AuthenticationStateProvider {
  public override Task<AuthenticationState> GetAuthenticationStateAsync()=>Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(new[]{new Claim(ClaimTypes.Name,"test-admin"),new Claim(ClaimTypes.Role,"Admin")},"test"))));
 }
 [Fact]
 public async Task HousesAndShopsUseTheSamePaymentsAndContracts() {
  var cs=new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("RENTAL_TEST_CONNECTION")??@"Server=(localdb)\MSSQLLocalDB;Integrated Security=true;TrustServerCertificate=true;");
  cs.InitialCatalog="Chwaya_RentalTest_"+Guid.NewGuid().ToString("N");
  var options=new DbContextOptionsBuilder<RentalDbContext>().UseSqlServer(cs.ConnectionString).Options;
  var factory=new Factory(options);var service=new RentalService(new EfRentalRepository(factory,new HtmlContractRenderer()),new BlazorCurrentUser(new Auth()));
  await using var verify=factory.CreateDbContext();
  try {
   Assert.False(verify.Database.HasPendingModelChanges());
   await verify.Database.MigrateAsync();
   await service.SaveAsync(new Landlord{Name="Test landlord",Address="Test area",Phone="000000",BankName="Test bank",AccountName="Test account",AccountNumber="001122"});
   var tenant=new Tenant{FullName="Test <script>alert(1)</script>",NationalId="TEST-001",Phone="000000",Address="Test area",AccountNumber="000123"};
   await service.SaveAsync(tenant);
   foreach(var kind in new[]{"House","Shop"}) {
    var property=new Property{Kind=kind,Name="Test "+kind,BuildingNumber="TEST-01",Address="Test area",Bedrooms=kind=="House"?2:0,MonthlyRent=650000,BusinessType=kind=="Shop"?"Retail":""};
    await service.SaveAsync(property);
    var lease=new Lease{PropertyId=property.Id,TenantId=tenant.Id,StartDate=new(2026,1,1),EndDate=new(2026,12,31),MonthlyRent=650000,Deposit=650000,LandlordWitnessName="Landlord witness",LandlordWitnessPhone="000001",WitnessName="Tenant witness",WitnessPhone="000002"};
    await service.SaveAsync(lease);
    await Assert.ThrowsAsync<InvalidOperationException>(()=>service.SaveAsync(new Lease{PropertyId=property.Id,TenantId=tenant.Id,StartDate=lease.StartDate,EndDate=lease.EndDate,MonthlyRent=650000}));
    await using(var direct=factory.CreateDbContext()) {
     direct.Leases.Add(new Lease{PropertyId=property.Id,TenantId=tenant.Id,StartDate=lease.StartDate,EndDate=lease.EndDate,MonthlyRent=650000});
     await Assert.ThrowsAsync<DbUpdateException>(()=>direct.SaveChangesAsync());
    }
    await service.SaveAsync(new Payment{LeaseId=lease.Id,Amount=650000,PaidOn=new(2026,10,5),Period=new(2026,10,1),Reference="PAY-"+kind});
    var contractId=await service.GenerateContractAsync(lease.Id);
    var html=await verify.Contracts.AsNoTracking().Where(x=>x.Id==contractId).Select(x=>x.Html).SingleAsync();
    Assert.Contains("&lt;script&gt;",html);Assert.DoesNotContain("<script>alert(1)</script>",html);Assert.Contains("001122",html);
    Assert.Contains(kind=="Shop"?"commercial shop use":"residential use",html);
    await Assert.ThrowsAsync<RentalPersistenceException>(()=>service.DeleteAsync(property));
    var original=html;tenant.Phone="001111";await service.SaveAsync(tenant);
    Assert.Equal(original,await verify.Contracts.AsNoTracking().Where(x=>x.Id==contractId).Select(x=>x.Html).SingleAsync());
   }
   var data=await service.LoadAsync();
   Assert.Equal(2,data.Payments.Count);Assert.Equal(2,data.Contracts.Count);
   Assert.Equal(1300000m,data.Payments.Sum(x=>x.Amount));
   Assert.Contains("000123",CsvExport.Records(data.Tenants));
   var accounts=new AdminAccountService(factory);
   Assert.True(await accounts.CreateAsync("administrator","InitialPassword2026!"));
   Assert.False(await accounts.CreateAsync("second-admin","InitialPassword2026!"));
   var session=await accounts.VerifyAsync("administrator","InitialPassword2026!");Assert.NotNull(session);
   Assert.False(await accounts.RenameAsync("administrator","wrong-password","rental-admin"));
   Assert.True(await accounts.RenameAsync("administrator","InitialPassword2026!","rental-admin"));
   Assert.False(await accounts.ValidateAsync("administrator",session.SecurityStamp));
   Assert.Null(await accounts.VerifyAsync("administrator","InitialPassword2026!"));
   Assert.NotNull(await accounts.VerifyAsync("rental-admin","InitialPassword2026!"));
   var renamed=await accounts.VerifyAsync("rental-admin","InitialPassword2026!");
   Assert.False(await accounts.ChangePasswordAsync("rental-admin","wrong-password","UpdatedPassword2026!"));
   Assert.True(await accounts.ChangePasswordAsync("rental-admin","InitialPassword2026!","UpdatedPassword2026!"));
   Assert.False(await accounts.ValidateAsync("rental-admin",renamed!.SecurityStamp));
   Assert.NotNull(await accounts.VerifyAsync("rental-admin","UpdatedPassword2026!"));
   await accounts.RecordSignOutAsync("rental-admin");
   var activity=await service.ActivityAsync(new(Action:"RenameAdmin",PageSize:1));Assert.Equal(1,activity.Total);Assert.Single(activity.Items);Assert.Equal("administrator",activity.Items[0].Actor);
   Assert.Single((await service.ActivityAsync(new(Action:"ChangePassword"))).Items);
   Assert.Empty((await service.ActivityAsync(new(Search:"no-such-actor"))).Items);
   var paged=await service.ActivityAsync(new(PageSize:2));Assert.Equal(2,paged.Items.Count);Assert.True(paged.Total>2);
   var second=await service.ActivityAsync(new(Page:2,PageSize:2));Assert.DoesNotContain(second.Items,x=>paged.Items.Any(y=>y.Id==x.Id));
   Assert.Empty((await service.ActivityAsync(new(From:new(2020,1,1),To:new(2020,12,31)))).Items);
   Assert.DoesNotContain("Password",string.Join(" ",paged.Items.Select(x=>x.Actor)));
   var workspace=new WorkspaceService(new EfWorkspaceRepository(factory),new CurrentUser());
   await workspace.SaveProfileAsync(new(){DisplayName="Rental owner",Email="owner@example.com",Phone="0123456789"});
   var summary=await workspace.SummaryAsync();Assert.Equal("Rental owner",summary.Profile.DisplayName);Assert.Equal("rental-admin",summary.Profile.Username);Assert.Equal("owner@example.com",summary.Profile.Email);
   Assert.True(await accounts.ValidateAsync("rental-admin",(await accounts.VerifyAsync("rental-admin","UpdatedPassword2026!"))!.SecurityStamp));
   await workspace.MarkReadAsync(null);Assert.Equal(0,(await workspace.SummaryAsync()).Unread);
   await workspace.AddCommentAsync(new(){Subject="Maintenance follow-up",Message="<script>alert(1)</script> inspect the door"});
   var comments=await workspace.CommentsAsync(new(Status:"open"));Assert.Single(comments.Items);Assert.Equal("rental-admin",comments.Items[0].Author);Assert.Contains("<script>",comments.Items[0].Message);
   var notices=await workspace.NotificationsAsync(true,1);Assert.Single(notices.Items);Assert.Equal("/comments",notices.Items[0].Link);Assert.Equal(1,notices.Unread);
   await workspace.MarkReadAsync(notices.Items[0].Id);Assert.Equal(0,(await workspace.SummaryAsync()).Unread);Assert.Single((await workspace.NotificationsAsync(false,1)).Items,x=>x.Id==notices.Items[0].Id&&x.IsRead);
   await workspace.ResolveCommentAsync(comments.Items[0].Id,true);Assert.Empty((await workspace.CommentsAsync(new(Status:"open"))).Items);Assert.Single((await workspace.CommentsAsync(new(Status:"resolved"))).Items);
   await workspace.ResolveCommentAsync(comments.Items[0].Id,false);Assert.Single((await workspace.CommentsAsync(new(Search:"door",Status:"open"))).Items);
   for(var i=0;i<26;i++)await workspace.AddCommentAsync(new(){Subject=$"Follow-up {i}",Message="Check rental maintenance"});
   var firstComments=await workspace.CommentsAsync(new());var nextComments=await workspace.CommentsAsync(new(Page:2));Assert.Equal(27,firstComments.Total);Assert.Equal(25,firstComments.Items.Count);Assert.Equal(2,nextComments.Items.Count);Assert.DoesNotContain(nextComments.Items,x=>firstComments.Items.Any(y=>x.Id==y.Id));
   var firstNotifications=await workspace.NotificationsAsync(true,1);var nextNotifications=await workspace.NotificationsAsync(true,2);Assert.Equal(26,firstNotifications.Unread);Assert.Equal(25,firstNotifications.Items.Count);Assert.Single(nextNotifications.Items);
   await workspace.MarkReadAsync(null);Assert.Empty((await workspace.NotificationsAsync(true,1)).Items);
   Assert.Single((await service.ActivityAsync(new(Action:"UpdateProfile"))).Items);
   Assert.Equal(27,(await service.ActivityAsync(new(Action:"CreateComment"))).Total);
   var clientAccounts=new ClientAccountService(factory);var clientRepository=new EfClientPortalRepository(factory);
   await clientAccounts.CreateAsync(new(){TenantId=tenant.Id,Username="tenant-one",TemporaryPassword="ClientInitial_2026!"},"test-admin");
   var clientSession=await clientAccounts.VerifyAsync("tenant-one","ClientInitial_2026!");Assert.NotNull(clientSession);Assert.True(clientSession.MustChangePassword);
   Assert.False(await clientAccounts.ChangePasswordAsync(clientSession.AccountId,"wrong-password","ClientUpdated_2026!"));Assert.True(await clientAccounts.ChangePasswordAsync(clientSession.AccountId,"ClientInitial_2026!","ClientUpdated_2026!"));
   Assert.False(await clientAccounts.ValidateAsync(clientSession.AccountId,tenant.Id,"tenant-one",clientSession.Stamp));clientSession=await clientAccounts.VerifyAsync("tenant-one","ClientUpdated_2026!");Assert.False(clientSession!.MustChangePassword);
   var client=new ClientPortalService(clientRepository,new ClientUser(new(clientSession.AccountId,tenant.Id,"tenant-one")),new FixedClock());
   var clientData=await client.LoadAsync();Assert.Equal(2,clientData.Leases.Count);Assert.Equal(2,clientData.Contracts.Count);Assert.Equal(2,client.Reminders(clientData).Count);
   var otherTenant=new Tenant{FullName="Other client",NationalId="OTHER-CLIENT",Phone="000111",Address="Other area"};await service.SaveAsync(otherTenant);
   var otherProperty=new Property{Name="Other private house",Address="Other area",MonthlyRent=500000};await service.SaveAsync(otherProperty);
   var otherLease=new Lease{PropertyId=otherProperty.Id,TenantId=otherTenant.Id,StartDate=new(2026,1,1),EndDate=new(2026,12,31),MonthlyRent=500000};await service.SaveAsync(otherLease);var privateContract=await service.GenerateContractAsync(otherLease.Id);
   Assert.Null(await client.ContractAsync(privateContract));Assert.NotNull(await client.ContractAsync(clientData.Contracts[0].Id));
   await Assert.ThrowsAsync<InvalidOperationException>(()=>client.SubmitAsync(new(){LeaseId=otherLease.Id,ContinueRent=true,Months=6}));
   var ownLease=clientData.Leases[0];await client.SubmitAsync(new(){LeaseId=ownLease.Id,ContinueRent=true,Months=6,Message="I would like to continue"});
   var request=Assert.Single(await clientRepository.RequestsAsync());Assert.False(request.Reviewed);Assert.Equal(tenant.FullName,request.Tenant);Assert.Equal(ownLease.End,(await client.LoadAsync()).Leases.Single(x=>x.Id==ownLease.Id).End);
   await clientRepository.ReviewAsync(request.Id,"Please contact the landlord to agree the new term.","test-admin");var reviewed=Assert.Single((await client.LoadAsync()).Requests);Assert.True(reviewed.Reviewed);Assert.Contains("agree",reviewed.Response);
   await client.SubmitAsync(new(){LeaseId=ownLease.Id,ContinueRent=false,Message="I plan to leave"});var updated=Assert.Single((await client.LoadAsync()).Requests);Assert.False(updated.ContinueRent);Assert.False(updated.Reviewed);Assert.Equal(0,updated.Months);
   await clientAccounts.EnableAsync(clientSession.AccountId,false,"test-admin");Assert.Null(await clientAccounts.VerifyAsync("tenant-one","ClientUpdated_2026!"));Assert.False(await clientAccounts.ValidateAsync(clientSession.AccountId,tenant.Id,"tenant-one",clientSession.Stamp));
   await clientAccounts.EnableAsync(clientSession.AccountId,true,"test-admin");await clientAccounts.ResetAsync(clientSession.AccountId,"ClientReset_2026!","test-admin");Assert.True((await clientAccounts.VerifyAsync("tenant-one","ClientReset_2026!"))!.MustChangePassword);
   Assert.Single(await clientAccounts.ListAsync());
   var reassigned=data.Leases.First(x=>x.Id==ownLease.Id);reassigned.TenantId=otherTenant.Id;await Assert.ThrowsAsync<InvalidOperationException>(()=>service.SaveAsync(reassigned));
  } finally { await verify.Database.EnsureDeletedAsync(); }
 }
 [Fact]
 public void ContractTermsAndBankNumbersAreEscaped() {
  var html=Contract.Render(new Lease{Id=1},new Property{Kind="Shop",BusinessType="Retail"},new Tenant{NationalId="00123"},new Landlord{AccountNumber="0000123",ContractTerms="<script>bad()</script>"});
  Assert.Contains("commercial shop use",html);Assert.Contains("0000123",html);Assert.Contains("&lt;script&gt;bad()&lt;/script&gt;",html);
 }
}
