using RentalManager;
using RentalManager.Services;
using Xunit;

public class RentalReportTests {
 [Fact]
 public void ReportsSeparateRentalMonthFromReceiptMonthAndFilterTenant() {
  var data = new RentalData {
   Tenants = [new Tenant { Id=1, FullName="Ali" }, new Tenant { Id=2, FullName="Asha" }],
   Properties = [new Property { Id=1, Name="House", Kind="House" }, new Property { Id=2, Name="Shop", Kind="Shop" }],
   Leases = [new Lease { Id=1, TenantId=1, PropertyId=1, StartDate=new(2026,1,1), EndDate=new(2026,12,31), MonthlyRent=100 },
    new Lease { Id=2, TenantId=2, PropertyId=2, StartDate=new(2026,1,1), EndDate=new(2026,12,31), MonthlyRent=200 },
    new Lease { Id=3, TenantId=1, PropertyId=1, Status="Cancelled", StartDate=new(2026,1,1), EndDate=new(2026,12,31), MonthlyRent=100 }],
   Payments = [new Payment { LeaseId=1, Period=new(2026,10,1), PaidOn=new(2026,9,30), Amount=120 },
    new Payment { LeaseId=2, Period=new(2026,9,1), PaidOn=new(2026,10,1), Amount=50 },
    new Payment { LeaseId=1, Kind="Deposit", Period=new(2026,10,1), PaidOn=new(2026,10,1), Amount=100 }]
  };
  var month = new DateOnly(2026,10,1);
  var all = RentalReports.Rent(data, month);
  Assert.Equal(2, all.Count);
  var person = Assert.Single(RentalReports.Rent(data, month, 1));
  Assert.Equal(120m, person.Paid); Assert.Equal(0m, person.Balance); Assert.Equal(20m, person.Credit);
  Assert.Equal(200m, all.Single(r => r.TenantId==2).Balance);
  Assert.Equal(2, RentalReports.Receipts(data, month).Count);
  Assert.Equal("Deposit", Assert.Single(RentalReports.Receipts(data, month, 1)).Kind);
  Assert.Empty(RentalReports.Rent(data, new(2027,1,1)));
 }
 [Fact]
 public void ExportEscapesNamesAndIncludesMonthAndPerson() {
  var csv = RentalReports.Csv([new(1,2,"=cmd,\"test\"", "Shop", "Shop", 100, 0, new(2026,10,1))], new(2026,10,1), "TZS");
  Assert.Contains("2026-10", csv); Assert.Contains("\"'=cmd,\"\"test\"\"\"", csv); Assert.Contains("Tenant ID", csv);
 }
 [Fact]
 public void AnnualTotalsOnlyIncludeCoveredMonthsAndSelectedPerson() {
  var data = new RentalData {
   Leases = [new Lease{Id=1,TenantId=1,StartDate=new(2026,3,15),EndDate=new(2026,5,10),MonthlyRent=100},
    new Lease{Id=2,TenantId=2,StartDate=new(2026,1,1),EndDate=new(2026,12,31),MonthlyRent=200}],
   Payments = [new Payment{LeaseId=1,Period=new(2026,3,1),PaidOn=new(2026,2,28),Amount=100}]
  };
  var annual=RentalReports.Annual(data,2026,1);
  Assert.Equal(12,annual.Count);Assert.Equal(300m,annual.Sum(r=>r.Rent));
  Assert.Equal(100m,annual[2].Paid);Assert.Equal(100m,annual[1].Received);
  Assert.Equal(200m,annual.Sum(r=>r.Balance));Assert.Equal(0m,annual[0].Rent);
 }
}
