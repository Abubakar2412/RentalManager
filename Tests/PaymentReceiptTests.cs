using RentalManager;
using RentalManager.Services;
using Xunit;
public class PaymentReceiptTests {
 [Fact] public void AnnualPaymentCombinesAcrossCalendarYears() {
  var parts=RentAllocation.Split(new Payment{LeaseId=2,Period=new(2026,10,1),PaidOn=new(2026,10,7),Amount=4200000,Reference="000012"},12);
  for(var i=0;i<parts.Count;i++)parts[i].Id=i+1;
  var receipt=Assert.Single(PaymentReceipts.Group(parts));
  Assert.Equal("000012",receipt.Reference);Assert.Equal(4200000m,receipt.Total);
  Assert.Equal(new DateOnly(2027,9,1),receipt.End);Assert.Equal(12,receipt.Payments.Count);
 }
 [Fact] public void MissingMonthsAndSeparatePaymentsRemainSeparate() {
  var parts=RentAllocation.Split(new Payment{LeaseId=2,Period=new(2026,10,1),Amount=600,Reference="P"},6);
  for(var i=0;i<parts.Count;i++)parts[i].Id=i+1;
  Assert.Equal(5,PaymentReceipts.Group(parts.Take(5)).Count);
  parts[1].PaidOn=parts[1].PaidOn.AddDays(1);
  Assert.Equal(6,PaymentReceipts.Group(parts).Count);
 }
}
