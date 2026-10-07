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
    var property=new Property{Kind=kind,Name="Test "+kind,Address="Test area",Bedrooms=kind=="House"?2:0,MonthlyRent=650000,BusinessType=kind=="Shop"?"Retail":""};
    await service.SaveAsync(property);
    var lease=new Lease{PropertyId=property.Id,TenantId=tenant.Id,StartDate=new(2026,1,1),EndDate=new(2026,12,31),MonthlyRent=650000,Deposit=650000};
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
  } finally { await verify.Database.EnsureDeletedAsync(); }
 }
 [Fact]
 public void ContractTermsAndBankNumbersAreEscaped() {
  var html=Contract.Render(new Lease{Id=1},new Property{Kind="Shop",BusinessType="Retail"},new Tenant{NationalId="00123"},new Landlord{AccountNumber="0000123",ContractTerms="<script>bad()</script>"});
  Assert.Contains("commercial shop use",html);Assert.Contains("0000123",html);Assert.Contains("&lt;script&gt;bad()&lt;/script&gt;",html);
 }
}
