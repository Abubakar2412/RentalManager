using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RentalManager.Data;
namespace RentalManager.Migrations;
[DbContext(typeof(RentalDbContext))]
[Migration("20261007083000_ContractRequirements")]
public class ContractRequirements : Migration {
 protected override void Up(MigrationBuilder m) {
  m.AddColumn<string>(name:"BuildingNumber",table:"Properties",type:"nvarchar(120)",maxLength:120,nullable:false,defaultValue:"");
  m.AddColumn<int>(name:"PaymentIntervalMonths",table:"Leases",type:"int",nullable:false,defaultValue:1);
  m.AddColumn<int>(name:"RenewalIntervalMonths",table:"Leases",type:"int",nullable:false,defaultValue:6);
  foreach(var name in new[]{"LandlordWitnessName","LandlordWitnessPhone","LandlordWitnessTitle","TenantWitnessTitle"}) {
   var length=name.EndsWith("Phone")?40:120;
   m.AddColumn<string>(name:name,table:"Leases",type:$"nvarchar({length})",maxLength:length,nullable:false,defaultValue:"");
  }
 }
 protected override void Down(MigrationBuilder m) {
  m.DropColumn(name:"BuildingNumber",table:"Properties");
  foreach(var name in new[]{"PaymentIntervalMonths","RenewalIntervalMonths","LandlordWitnessName","LandlordWitnessPhone","LandlordWitnessTitle","TenantWitnessTitle"})m.DropColumn(name:name,table:"Leases");
 }
}
