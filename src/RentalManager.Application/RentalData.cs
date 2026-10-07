namespace RentalManager.Services;
public record ContractInfo(int Id,int LeaseId,DateTimeOffset CreatedAt);
public class RentalData {
 public List<Property> Properties {get;set;}=[];
 public List<Tenant> Tenants {get;set;}=[];
 public List<Lease> Leases {get;set;}=[];
 public List<Payment> Payments {get;set;}=[];
 public Landlord Landlord {get;set;}=new();
 public List<ContractInfo> Contracts {get;set;}=[];
 public List<AuditEntry> Audit {get;set;}=[];
 public string CurrencyCode => System.Text.RegularExpressions.Regex.IsMatch(Landlord.Currency ?? "", @"\A[A-Za-z]{3,10}\z") ? Landlord.Currency.ToUpperInvariant() : "TZS";
 public string Money(decimal amount)=>$"{CurrencyCode} {amount:N2}";
 public string PropertyName(int id)=>Properties.FirstOrDefault(x=>x.Id==id)?.Name??"Unknown property";
 public string TenantName(int id)=>Tenants.FirstOrDefault(x=>x.Id==id)?.FullName??"Unknown tenant";
 public bool IsCurrent(Lease l)=>l.Status=="Active"&&l.StartDate<=DateOnly.FromDateTime(DateTime.Today)&&l.EndDate>=DateOnly.FromDateTime(DateTime.Today);
}
