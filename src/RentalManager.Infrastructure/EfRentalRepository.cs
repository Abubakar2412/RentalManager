using System.ComponentModel.DataAnnotations;
using System.Data;
using Microsoft.EntityFrameworkCore;
using RentalManager.Data;
namespace RentalManager.Services;
public class EfRentalRepository(IDbContextFactory<RentalDbContext> factory,IContractRenderer renderer) : IRentalRepository {
 public Task<ActivityLogPage> ActivityAsync(ActivityLogFilter filter,string actor)=>Guard(async()=>{
  filter.Validate();await using var db=await factory.CreateDbContextAsync();var query=db.Audit.AsNoTracking().AsQueryable();
  if(!string.IsNullOrWhiteSpace(filter.Search)){var term=filter.Search.Trim();query=query.Where(x=>x.Actor.Contains(term)||x.Entity.Contains(term));}
  if(!string.IsNullOrWhiteSpace(filter.Action))query=query.Where(x=>x.Action==filter.Action);
  if(filter.From is {} from){var start=new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue),TimeSpan.Zero);query=query.Where(x=>x.At>=start);}
  if(filter.To is {} to){var end=new DateTimeOffset(to.ToDateTime(TimeOnly.MaxValue),TimeSpan.Zero);query=query.Where(x=>x.At<=end);}
  var total=await query.CountAsync();var items=await query.OrderByDescending(x=>x.At).ThenByDescending(x=>x.Id).Skip((filter.Page-1)*filter.PageSize).Take(filter.PageSize).ToListAsync();return new ActivityLogPage(items,total,filter.Page,filter.PageSize);
 });
 public Task<RentalData> LoadAsync(string actor,RentalLoadScope scope)=>Guard(()=>LoadAsyncCore(actor,scope));
 public Task SaveAsync(IEntity entity,string actor)=>Guard(()=>SaveAsyncCore(entity,actor));
 public Task SaveRentInstalmentAsync(Payment payment,int months,string actor)=>Guard(()=>SaveRentInstalmentAsyncCore(payment,months,actor));
 public Task DeleteAsync(IEntity entity,string actor)=>Guard(()=>DeleteAsyncCore(entity,actor));
 public Task<int> GenerateContractAsync(int id,string actor)=>Guard(()=>GenerateContractAsyncCore(id,actor));
 static async Task<T> Guard<T>(Func<Task<T>> work){try{return await work();}catch(DbUpdateConcurrencyException e){throw new RentalPersistenceException("Another session changed this record. Refresh and try again.",e);}catch(DbUpdateException e){throw new RentalPersistenceException("Cannot save or delete this record: check duplicate IDs, overlapping dates and linked records.",e);}}
 static async Task Guard(Func<Task> work){await Guard(async()=>{await work();return true;});}

 async Task<RentalData> LoadAsyncCore(string actor,RentalLoadScope scope){await using var db=await factory.CreateDbContextAsync();return new RentalData{
  Properties=scope==RentalLoadScope.Audit ? [] : await db.Properties.AsNoTracking().OrderByDescending(x=>x.Id).ToListAsync(),
  Tenants=scope==RentalLoadScope.Audit ? [] : await db.Tenants.AsNoTracking().OrderByDescending(x=>x.Id).ToListAsync(),
  Leases=scope==RentalLoadScope.Audit ? [] : await db.Leases.AsNoTracking().OrderByDescending(x=>x.Id).ToListAsync(),
  Payments=scope==RentalLoadScope.Audit ? [] : await db.Payments.AsNoTracking().OrderByDescending(x=>x.Id).ToListAsync(),
  Landlord=await db.Landlords.AsNoTracking().SingleOrDefaultAsync()??new(),
  Contracts=scope is RentalLoadScope.All or RentalLoadScope.Contracts ? await db.Contracts.AsNoTracking().OrderByDescending(x=>x.Id).Select(x=>new ContractInfo(x.Id,x.LeaseId,x.CreatedAt)).ToListAsync() : [],
  Audit=scope is RentalLoadScope.All or RentalLoadScope.Audit ? await db.Audit.AsNoTracking().OrderByDescending(x=>x.Id).Take(250).ToListAsync() : []
 };}
 static void Validate(object entity){var errors=new List<ValidationResult>();if(!Validator.TryValidateObject(entity,new ValidationContext(entity),errors,true))throw new InvalidOperationException(string.Join(" ",errors.Select(x=>x.ErrorMessage)));}
 static void AddAudit(RentalDbContext db,string actor,string action,string entity,int id)=>db.Audit.Add(new(){At=DateTimeOffset.UtcNow,Actor=actor,Action=action,Entity=entity,EntityId=id});
 async Task SaveAsyncCore(IEntity entity,string actor){
  Validate(entity);
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
  if(entity is Payment payment){
   if(payment.PaidOn==default||payment.Period==default)throw new InvalidOperationException("Payment date and rental period are required.");
   if(decimal.Round(payment.Amount,2)!=payment.Amount)throw new InvalidOperationException("Payment amount must have at most two decimal places.");
   var lease=await db.Leases.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==payment.LeaseId)??throw new InvalidOperationException("Select an existing lease.");
   if(payment.Kind=="Rent"){
    var period=new DateOnly(payment.Period.Year,payment.Period.Month,1);var first=new DateOnly(lease.StartDate.Year,lease.StartDate.Month,1);var last=new DateOnly(lease.EndDate.Year,lease.EndDate.Month,1);
    if(lease.Status=="Cancelled"||period<first||period>last)throw new InvalidOperationException("Rental month must fall within a non-cancelled lease.");
    payment.Period=period;
   }
  }
  if(added)db.Add(entity);else db.Update(entity);
  await db.SaveChangesAsync();AddAudit(db,actor,added?"Create":"Update",entity.GetType().Name,entity.Id);await db.SaveChangesAsync();await tx.CommitAsync();
 }
 async Task SaveRentInstalmentAsyncCore(Payment payment,int months,string actor) {
  if(payment.Id!=0||payment.Kind!="Rent"||months<1||months>12)throw new InvalidOperationException("Select 1–12 months for a new rent payment.");
  Validate(payment);
  if(payment.PaidOn==default||payment.Period==default)throw new InvalidOperationException("Payment date and rental month are required.");
  await using var db=await factory.CreateDbContextAsync();
  await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
  var lease=await db.Leases.SingleOrDefaultAsync(x=>x.Id==payment.LeaseId)??throw new InvalidOperationException("Select an existing lease.");
  var parts=RentAllocation.Split(payment,months);
  var first=new DateOnly(lease.StartDate.Year,lease.StartDate.Month,1);var last=new DateOnly(lease.EndDate.Year,lease.EndDate.Month,1);
  if(lease.Status=="Cancelled"||parts.Any(x=>x.Period<first||x.Period>last))throw new InvalidOperationException("All rental months must fall within a non-cancelled lease.");
  foreach(var part in parts){Validate(part);db.Payments.Add(part);}
  await db.SaveChangesAsync();foreach(var part in parts)AddAudit(db,actor,"Create","Payment",part.Id);
  await db.SaveChangesAsync();await tx.CommitAsync();
 }
 async Task DeleteAsyncCore(IEntity entity,string actor){if(entity is Landlord)throw new InvalidOperationException("Landlord settings cannot be deleted.");await using var db=await factory.CreateDbContextAsync();await using var tx=await db.Database.BeginTransactionAsync();db.Remove(entity);AddAudit(db,actor,"Delete",entity.GetType().Name,entity.Id);await db.SaveChangesAsync();await tx.CommitAsync();}
 async Task<int> GenerateContractAsyncCore(int leaseId,string actor){
  await using var db=await factory.CreateDbContextAsync();await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
  var l=await db.Leases.AsNoTracking().SingleAsync(x=>x.Id==leaseId);
  var p=await db.Properties.AsNoTracking().SingleAsync(x=>x.Id==l.PropertyId);
  var t=await db.Tenants.AsNoTracking().SingleAsync(x=>x.Id==l.TenantId);
  var landlord=await db.Landlords.AsNoTracking().SingleOrDefaultAsync()??throw new InvalidOperationException("Complete Landlord & settings first.");Validate(landlord);
  if(p.Kind=="Shop"&&(string.IsNullOrWhiteSpace(p.BuildingNumber)||string.IsNullOrWhiteSpace(l.LandlordWitnessName)||string.IsNullOrWhiteSpace(l.WitnessName)||string.IsNullOrWhiteSpace(l.LandlordWitnessPhone)||string.IsNullOrWhiteSpace(l.WitnessPhone)))throw new InvalidOperationException("Complete the building number and both witnesses' names and phone numbers before generating a shop contract.");
  var doc=new ContractSnapshot{LeaseId=leaseId,CreatedAt=DateTimeOffset.UtcNow,Html=renderer.Render(l,p,t,landlord)};
  db.Contracts.Add(doc);await db.SaveChangesAsync();AddAudit(db,actor,"Generate","Contract",doc.Id);await db.SaveChangesAsync();await tx.CommitAsync();return doc.Id;
 }
}
