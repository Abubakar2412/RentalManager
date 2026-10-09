using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RentalManager.Data;
using Xunit;
public class WorkspaceUpgradeTests {
 [Fact]public async Task HousesAndShopsUseTheSamePaymentsAndContractsUpgradePreservesExistingAdmin(){
  var cs=new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("RENTAL_TEST_CONNECTION")??@"Server=(localdb)\MSSQLLocalDB;Integrated Security=true;TrustServerCertificate=true;"){InitialCatalog="Chwaya_UpgradeTest_"+Guid.NewGuid().ToString("N")};
  await using var db=new RentalDbContext(new DbContextOptionsBuilder<RentalDbContext>().UseSqlServer(cs.ConnectionString).Options);
  try{
   await db.GetService<IMigrator>().MigrateAsync("20261007133000_EnforceLeaseOverlap");
   await db.Database.ExecuteSqlRawAsync("INSERT INTO [Admins] ([Id],[Username],[PasswordHash],[SecurityStamp]) VALUES (1,'existing-owner','test-only-hash','test-only-stamp')");
   await db.Database.MigrateAsync();Assert.False(db.Database.HasPendingModelChanges());
   var admin=await db.Admins.AsNoTracking().SingleAsync();Assert.Equal("existing-owner",admin.Username);Assert.Equal("test-only-hash",admin.PasswordHash);Assert.Equal("test-only-stamp",admin.SecurityStamp);Assert.Equal("",admin.DisplayName);Assert.Null(admin.Email);
   Assert.Empty(await db.Comments.ToListAsync());Assert.Empty(await db.Notifications.ToListAsync());
  }finally{await db.Database.EnsureDeletedAsync();}
 }
}
