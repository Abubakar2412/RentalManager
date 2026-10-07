using System.Globalization;
namespace RentalManager.Services;

public record MonthlyRentRow(int LeaseId, int TenantId, string Tenant, string Property, string Type,
 decimal Rent, decimal Paid, DateOnly DueDate) {
 public decimal Balance => Math.Max(0, Rent - Paid);
 public decimal Credit => Math.Max(0, Paid - Rent);
}

public record AnnualRentRow(DateOnly Month, decimal Rent, decimal Paid, decimal Balance, decimal Credit, decimal Received);

public static class RentalReports {
 public static List<AnnualRentRow> Annual(RentalData data, int year, int? tenantId = null) {
  if(year < 1 || year >= 9999) throw new ArgumentOutOfRangeException(nameof(year));
  return Enumerable.Range(1,12).Select(m => {
   var month=new DateOnly(year,m,1);var rows=Rent(data,month,tenantId);
   return new AnnualRentRow(month,rows.Sum(r=>r.Rent),rows.Sum(r=>r.Paid),rows.Sum(r=>r.Balance),rows.Sum(r=>r.Credit),Receipts(data,month,tenantId).Sum(p=>p.Amount));
  }).ToList();
 }

 public static List<MonthlyRentRow> Rent(RentalData data, DateOnly month, int? tenantId = null) {
  var start = new DateOnly(month.Year, month.Month, 1);
  var end = start.AddMonths(1).AddDays(-1);
  return data.Leases.Where(l => l.Status != "Cancelled" && l.StartDate <= end && l.EndDate >= start
    && (tenantId == null || l.TenantId == tenantId))
   .Select(l => new MonthlyRentRow(l.Id, l.TenantId, data.TenantName(l.TenantId), data.PropertyName(l.PropertyId),
    data.Properties.FirstOrDefault(p => p.Id == l.PropertyId)?.Kind ?? "Unknown",
    l.MonthlyRent, data.Payments.Where(p => p.LeaseId == l.Id && p.Kind == "Rent"
     && p.Period.Year == start.Year && p.Period.Month == start.Month).Sum(p => p.Amount),
    new DateOnly(start.Year, start.Month, l.DueDay) < l.StartDate ? l.StartDate : new DateOnly(start.Year, start.Month, l.DueDay)))
   .OrderBy(r => r.Tenant).ThenBy(r => r.Property).ThenBy(r => r.LeaseId).ToList();
 }
 public static List<Payment> Receipts(RentalData data, DateOnly month, int? tenantId = null) =>
  data.Payments.Where(p => p.PaidOn.Year == month.Year && p.PaidOn.Month == month.Month
   && (tenantId == null || data.Leases.Any(l => l.Id == p.LeaseId && l.TenantId == tenantId)))
   .OrderBy(p => p.PaidOn).ThenBy(p => p.Id).ToList();
 public static string Csv(IEnumerable<MonthlyRentRow> rows, DateOnly month, string currency) {
  static string Cell(object value) {
   var text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
   if(text.Length > 0 && "=+-@\t\r".Contains(text[0])) text = "'" + text;
   return "\"" + text.Replace("\"", "\"\"") + "\"";
  }
  var lines = new List<string> { "Month,Lease,Tenant ID,Tenant,Property,Type,Currency,Rent,Rent paid,Balance,Credit,Due date" };
  lines.AddRange(rows.Select(r => string.Join(",", new object[] { month.ToString("yyyy-MM"), r.LeaseId,
   r.TenantId, r.Tenant, r.Property, r.Type, currency, r.Rent, r.Paid, r.Balance, r.Credit, r.DueDate.ToString("yyyy-MM-dd") }.Select(Cell))));
  return "\uFEFF" + string.Join("\r\n", lines);
 }
}
