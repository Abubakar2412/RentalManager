using RentalManager;
using RentalManager.Services;
using Xunit;
public class RentAllocationTests {
 [Fact]
 public void SixMonthInstalmentIsNotCountedSixTimes() {
  var payment=new Payment{LeaseId=1,Period=new(2026,9,1),PaidOn=new(2026,9,1),Amount=1500000,Reference="SIX-MONTH"};
  var parts=RentAllocation.Split(payment,6);
  Assert.Equal(6,parts.Count);Assert.All(parts,p=>Assert.Equal(250000m,p.Amount));
  Assert.Equal(1500000m,parts.Sum(p=>p.Amount));Assert.Equal(new DateOnly(2027,2,1),parts.Last().Period);
  Assert.Equal(6,parts.Select(p=>p.Reference).Distinct().Count());
 }
 [Fact]
 public void RoundingPreservesTotal() {
  var parts=RentAllocation.Split(new Payment{Period=new(2026,1,1),Amount=100,Reference="P"},3);
  Assert.Equal(100m,parts.Sum(p=>p.Amount));Assert.Equal(33.34m,parts[0].Amount);
 }
 [Fact]
 public void EnglishContractContainsInstalmentAndBothWitnesses() {
  var html=Contract.Render(new Lease{MonthlyRent=250000,PaymentIntervalMonths=6,LandlordWitnessName="Owner witness",WitnessName="Tenant witness"},
   new Property{Kind="Shop",BuildingNumber="SH/1/NO6"},new Tenant(),new Landlord{Currency="TZS"});
  Assert.Contains("1,500,000",html);Assert.Contains("Owner witness",html);Assert.Contains("Tenant witness",html);
  Assert.Contains("must not sublet",html);Assert.Contains("peaceful use",html);
 }
}
