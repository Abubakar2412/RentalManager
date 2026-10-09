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
 sealed class CurrentUser : ICurrentUser {public Task<string> GetRequiredNameAsync()=>Task.FromResult("rental-admin");}
 sealed class Auth : AuthenticationStateProvider {
  public override Task<AuthenticationState> GetAuthenticationStateAsync()=>Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(new[]{new Claim(ClaimTypes.Name,"test-admin")},"test"))));
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
   await workspace.MarkReadAsync(notices.Items[0].Id);Assert.Equal(0,(await workspace.SummaryAsync()).Unread);Assert.Single((await workspace.NotificationsAsync(false,1)).Items.Where(x=>x.Id==notices.Items[0].Id&&x.IsRead));
   await workspace.ResolveCommentAsync(comments.Items[0].Id,true);Assert.Empty((await workspace.CommentsAsync(new(Status:"open"))).Items);Assert.Single((await workspace.CommentsAsync(new(Status:"resolved"))).Items);
   await workspace.ResolveCommentAsync(comments.Items[0].Id,false);Assert.Single((await workspace.CommentsAsync(new(Search:"door",Status:"open"))).Items);
   for(var i=0;i<26;i++)await workspace.AddCommentAsync(new(){Subject=$"Follow-up {i}",Message="Check rental maintenance"});
   var firstComments=await workspace.CommentsAsync(new());var nextComments=await workspace.CommentsAsync(new(Page:2));Assert.Equal(27,firstComments.Total);Assert.Equal(25,firstComments.Items.Count);Assert.Equal(2,nextComments.Items.Count);Assert.DoesNotContain(nextComments.Items,x=>firstComments.Items.Any(y=>x.Id==y.Id));
   var firstNotifications=await workspace.NotificationsAsync(true,1);var nextNotifications=await workspace.NotificationsAsync(true,2);Assert.Equal(26,firstNotifications.Unread);Assert.Equal(25,firstNotifications.Items.Count);Assert.Single(nextNotifications.Items);
   await workspace.MarkReadAsync(null);Assert.Empty((await workspace.NotificationsAsync(true,1)).Items);
   Assert.Single((await service.ActivityAsync(new(Action:"UpdateProfile"))).Items);
   Assert.Equal(27,(await service.ActivityAsync(new(Action:"CreateComment"))).Total);
  } finally { await verify.Database.EnsureDeletedAsync(); }
 }
 [Fact]
 public void ContractTermsAndBankNumbersAreEscaped() {
  var html=Contract.Render(new Lease{Id=1},new Property{Kind="Shop",BusinessType="Retail"},new Tenant{NationalId="00123"},new Landlord{AccountNumber="0000123",ContractTerms="<script>bad()</script>"});
  Assert.Contains("commercial shop use",html);Assert.Contains("0000123",html);Assert.Contains("&lt;script&gt;bad()&lt;/script&gt;",html);
 }
}
