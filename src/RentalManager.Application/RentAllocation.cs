namespace RentalManager.Services;
public static class RentAllocation {
 public static List<Payment> Split(Payment payment,int months) {
  if(months<1||months>12||payment.Period==default||payment.Amount<=0||decimal.Round(payment.Amount,2)!=payment.Amount)throw new InvalidOperationException("Enter a positive amount with at most two decimal places and 1–12 months.");
  var cents=decimal.ToInt64(payment.Amount*100);var each=cents/months;var remainder=cents%months;
  if(each==0)throw new InvalidOperationException("The total must allocate at least 0.01 to every month.");
  var start=new DateOnly(payment.Period.Year,payment.Period.Month,1);
  if(start.Year==9999&&start.Month+months-1>12)throw new InvalidOperationException("Invalid rental month range.");
  return Enumerable.Range(0,months).Select(i=>new Payment {
   LeaseId=payment.LeaseId,PaidOn=payment.PaidOn,Period=start.AddMonths(i),Amount=(each+(i<remainder?1:0))/100m,
   Kind="Rent",Method=payment.Method,Reference=months==1?payment.Reference:payment.Reference[..Math.Min(payment.Reference.Length,100)]+$"/{i+1}-{months}",Notes=payment.Notes
  }).ToList();
 }
}
