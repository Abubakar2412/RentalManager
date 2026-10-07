using System.ComponentModel.DataAnnotations;
using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Components.Authorization;
using RentalManager.Data;
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
public class RentalService(IDbContextFactory<RentalDbContext> factory,AuthenticationStateProvider authentication) {
 async Task<string> Actor(){var user=(await authentication.GetAuthenticationStateAsync()).User;if(user.Identity?.IsAuthenticated!=true)throw new UnauthorizedAccessException("Please sign in again.");return user.Identity.Name??"administrator";}
 public async Task<RentalData> LoadAsync(){await Actor();await using var db=await factory.CreateDbContextAsync();return new RentalData{
  Properties=await db.Properties.AsNoTracking().OrderByDescending(x=>x.Id).ToListAsync(),
  Tenants=await db.Tenants.AsNoTracking().OrderByDescending(x=>x.Id).ToListAsync(),
  Leases=await db.Leases.AsNoTracking().OrderByDescending(x=>x.Id).ToListAsync(),
  Payments=await db.Payments.AsNoTracking().OrderByDescending(x=>x.Id).ToListAsync(),
  Landlord=await db.Landlords.AsNoTracking().SingleOrDefaultAsync()??new(),
  Contracts=await db.Contracts.AsNoTracking().OrderByDescending(x=>x.Id).Select(x=>new ContractInfo(x.Id,x.LeaseId,x.CreatedAt)).ToListAsync(),
  Audit=await db.Audit.AsNoTracking().OrderByDescending(x=>x.Id).Take(250).ToListAsync()
 };}
 static void Validate(object entity){var errors=new List<ValidationResult>();if(!Validator.TryValidateObject(entity,new ValidationContext(entity),errors,true))throw new InvalidOperationException(string.Join(" ",errors.Select(x=>x.ErrorMessage)));}
 static void AddAudit(RentalDbContext db,string actor,string action,string entity,int id)=>db.Audit.Add(new(){At=DateTimeOffset.UtcNow,Actor=actor,Action=action,Entity=entity,EntityId=id});
 public async Task SaveAsync(IEntity entity){
  var actor=await Actor();Validate(entity);
  await using var db=await factory.CreateDbContextAsync();
  await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
  var added=entity.Id==0;
  if(entity is Landlord){entity.Id=1;added=!await db.Landlords.AnyAsync();}
  if(entity is Tenant tenant){tenant.NationalId=tenant.NationalId.Trim().ToUpperInvariant();if(await db.Tenants.AnyAsync(x=>x.Id!=tenant.Id&&x.NationalId==tenant.NationalId))throw new InvalidOperationException("This national ID already belongs to another tenant.");}
  if(entity is Property prop&&prop.Kind=="Shop"&&string.IsNullOrWhiteSpace(prop.BusinessType))throw new InvalidOperationException("Enter the permitted business activity for this shop.");
  if(entity is Lease l){
   if(l.StartDate==default||l.EndDate<l.StartDate)throw new InvalidOperationException("Enter valid start and end dates.");
   // Serialize reservations. A SQL Server trigger also protects direct writes.
   var property=await db.Properties.FromSqlInterpolated($"SELECT * FROM [Properties] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={l.PropertyId}").SingleOrDefaultAsync();
   if(property==null||!await db.Tenants.AnyAsync(x=>x.Id==l.TenantId))throw new InvalidOperationException("Select an existing property and tenant.");
   if(l.Status=="Active"&&!property.Available)throw new InvalidOperationException("The property is unavailable for leasing.");
   if(l.Status=="Active"&&await db.Leases.AnyAsync(x=>x.Id!=l.Id&&x.PropertyId==l.PropertyId&&x.Status=="Active"&&x.StartDate<=l.EndDate&&x.EndDate>=l.StartDate))throw new InvalidOperationException("This house or shop already has an overlapping active lease.");
   var old=await db.Leases.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==l.Id);
   if(old!=null&&(old.PropertyId!=l.PropertyId||old.TenantId!=l.TenantId)&&await db.Payments.AnyAsync(x=>x.LeaseId==l.Id))throw new InvalidOperationException("A lease with payments cannot be reassigned. Create a new lease.");
  }
  if(entity is Payment payment){if(payment.PaidOn==default||payment.Period==default)throw new InvalidOperationException("Payment date and rental period are required.");if(!await db.Leases.AnyAsync(x=>x.Id==payment.LeaseId))throw new InvalidOperationException("Select an existing lease.");}
  if(added)db.Add(entity);else db.Update(entity);
  await db.SaveChangesAsync();AddAudit(db,actor,added?"Create":"Update",entity.GetType().Name,entity.Id);await db.SaveChangesAsync();await tx.CommitAsync();
 }
 public async Task DeleteAsync(IEntity entity){var actor=await Actor();if(entity is Landlord)throw new InvalidOperationException("Landlord settings cannot be deleted.");await using var db=await factory.CreateDbContextAsync();await using var tx=await db.Database.BeginTransactionAsync();db.Remove(entity);AddAudit(db,actor,"Delete",entity.GetType().Name,entity.Id);await db.SaveChangesAsync();await tx.CommitAsync();}
 public async Task<int> GenerateContractAsync(int leaseId){
  var actor=await Actor();await using var db=await factory.CreateDbContextAsync();await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
  var l=await db.Leases.AsNoTracking().SingleAsync(x=>x.Id==leaseId);
  var p=await db.Properties.AsNoTracking().SingleAsync(x=>x.Id==l.PropertyId);
  var t=await db.Tenants.AsNoTracking().SingleAsync(x=>x.Id==l.TenantId);
  var landlord=await db.Landlords.AsNoTracking().SingleOrDefaultAsync()??throw new InvalidOperationException("Complete Landlord & settings first.");Validate(landlord);
  var doc=new ContractSnapshot{LeaseId=leaseId,CreatedAt=DateTimeOffset.UtcNow,Html=Contract.Render(l,p,t,landlord)};
  db.Contracts.Add(doc);await db.SaveChangesAsync();AddAudit(db,actor,"Generate","Contract",doc.Id);await db.SaveChangesAsync();await tx.CommitAsync();return doc.Id;
 }
 public static string Error(Exception e)=>e switch{
  DbUpdateConcurrencyException=>"Another session changed this record. Close the editor, refresh, and try again.",
  DbUpdateException=>"Cannot save or delete this record: check duplicate IDs, overlapping dates and linked records.",
  InvalidOperationException=>e.Message,
  UnauthorizedAccessException=>e.Message,
  _=>"The operation failed. Check the server log or SQL Server connection."
 };
}
