using System.Text.RegularExpressions;
namespace RentalManager.Services;
public record PaymentReceipt(string Reference, List<Payment> Payments) {
 public Payment First => Payments[0];
 public decimal Total => Payments.Sum(p=>p.Amount);
 public DateOnly Start => Payments.Min(p=>p.Period);
 public DateOnly End => Payments.Max(p=>p.Period);
}
public static class PaymentReceipts {
 public static List<PaymentReceipt> Group(IEnumerable<Payment> payments) {
  var all=payments.ToList();var used=new HashSet<int>();var receipts=new List<PaymentReceipt>();
  var candidates=all.Where(p=>p.Kind=="Rent").Select(p=>(Payment:p,Match:Regex.Match(p.Reference,@"^(.*)/(\d+)-(\d+)$")))
   .Where(x=>x.Match.Success).GroupBy(x=>new{x.Payment.LeaseId,x.Payment.PaidOn,x.Payment.Method,x.Payment.Notes,Reference=x.Match.Groups[1].Value,Count=x.Match.Groups[3].Value});
  foreach(var group in candidates) {
   if(!int.TryParse(group.Key.Count,out var count)||count<2||count>12)continue;
   var items=group.ToList();
   if(items.Any(x=>!int.TryParse(x.Match.Groups[2].Value,out var index)||index<1||index>count))continue;
   if(items.Count!=count)continue;
   var ordered=items.OrderBy(x=>int.Parse(x.Match.Groups[2].Value)).ToList();
   var start=ordered[0].Payment.Period;
   if(!ordered.Select((x,i)=>x.Match.Groups[2].Value==(i+1).ToString() && (start.Year<9999 || start.Month+i<=12) && x.Payment.Period==start.AddMonths(i)).All(x=>x))continue;
   var parts=ordered.Select(x=>x.Payment).ToList();receipts.Add(new(group.Key.Reference,parts));foreach(var part in parts)used.Add(part.Id);
  }
  receipts.AddRange(all.Where(p=>!used.Contains(p.Id)).Select(p=>new PaymentReceipt(p.Reference,[p])));
  return receipts.OrderByDescending(r=>r.First.PaidOn).ThenByDescending(r=>r.Payments.Max(p=>p.Id)).ToList();
 }
}
