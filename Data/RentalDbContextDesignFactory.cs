using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace RentalManager.Data;
// Allows migration commands to inspect the model without running application startup migrations.
public sealed class RentalDbContextDesignFactory : IDesignTimeDbContextFactory<RentalDbContext> {
 public RentalDbContext CreateDbContext(string[] args) {
  var environment=Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
   ??Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")??"Development";
  var builder=new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
   .AddJsonFile("appsettings.json",optional:false)
   .AddJsonFile($"appsettings.{environment}.json",optional:true);
  if(environment=="Development")builder.AddUserSecrets<RentalDbContextDesignFactory>(optional:true);
  var configuration=builder.AddEnvironmentVariables().AddCommandLine(args).Build();
  var name=configuration["Database:ConnectionName"]??"DefaultConnectionOnHrPayMisHubDbDev";
  var connection=configuration.GetConnectionString(name)??throw new InvalidOperationException("Selected connection string is missing.");
  return new(new DbContextOptionsBuilder<RentalDbContext>().UseSqlServer(connection).Options);
 }
}
