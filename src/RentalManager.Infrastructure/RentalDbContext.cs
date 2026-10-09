using Microsoft.EntityFrameworkCore;
namespace RentalManager.Data;
public class RentalDbContext(DbContextOptions<RentalDbContext> options) : DbContext(options) {
 public DbSet<RentalComment> Comments=>Set<RentalComment>();
 public DbSet<AppNotification> Notifications=>Set<AppNotification>();
 public DbSet<Property> Properties=>Set<Property>();
 public DbSet<Tenant> Tenants=>Set<Tenant>();
 public DbSet<Lease> Leases=>Set<Lease>();
 public DbSet<Payment> Payments=>Set<Payment>();
 public DbSet<Landlord> Landlords=>Set<Landlord>();
 public DbSet<Admin> Admins=>Set<Admin>();
 public DbSet<ContractSnapshot> Contracts=>Set<ContractSnapshot>();
 public DbSet<AuditEntry> Audit=>Set<AuditEntry>();
 protected override void OnModelCreating(ModelBuilder b) {
  b.Entity<Property>().ToTable("Properties",t=>{t.HasCheckConstraint("CK_Properties_Kind","[Kind] IN ('House','Shop')");t.HasCheckConstraint("CK_Properties_Rent","[MonthlyRent] > 0");});
  b.Entity<Tenant>().ToTable("Tenants");b.Entity<Tenant>().HasIndex(t=>t.NationalId).IsUnique();
  b.Entity<Lease>().ToTable("Leases",t=>{t.UseSqlOutputClause(false);t.HasTrigger("TR_Leases_NoOverlap");t.HasCheckConstraint("CK_Leases_Dates","[EndDate] >= [StartDate]");t.HasCheckConstraint("CK_Leases_Status","[Status] IN ('Active','Ended','Cancelled')");t.HasCheckConstraint("CK_Leases_Amounts","[MonthlyRent]>0 AND [Deposit]>=0 AND [DueDay] BETWEEN 1 AND 28");});
  b.Entity<Lease>().HasOne<Property>().WithMany().HasForeignKey(l=>l.PropertyId).OnDelete(DeleteBehavior.Restrict);
  b.Entity<Lease>().HasOne<Tenant>().WithMany().HasForeignKey(l=>l.TenantId).OnDelete(DeleteBehavior.Restrict);
  b.Entity<Lease>().HasIndex(l=>new{l.PropertyId,l.StartDate,l.EndDate});
  b.Entity<Payment>().ToTable("Payments",t=>{t.HasCheckConstraint("CK_Payments_Amount","[Amount]>0");t.HasCheckConstraint("CK_Payments_Kind","[Kind] IN ('Rent','Deposit','Other')");});
  b.Entity<Payment>().HasOne<Lease>().WithMany().HasForeignKey(p=>p.LeaseId).OnDelete(DeleteBehavior.Restrict);
  b.Entity<Landlord>().ToTable("Landlords",t=>t.HasCheckConstraint("CK_Landlords_Singleton","[Id]=1"));b.Entity<Landlord>().Property(x=>x.Id).ValueGeneratedNever();
  b.Entity<Admin>().ToTable("Admins",t=>t.HasCheckConstraint("CK_Admins_Singleton","[Id]=1"));b.Entity<Admin>().Property(x=>x.Id).ValueGeneratedNever();b.Entity<Admin>().HasIndex(x=>x.Username).IsUnique();
  b.Entity<ContractSnapshot>().ToTable("Contracts");b.Entity<ContractSnapshot>().HasOne<Lease>().WithMany().HasForeignKey(x=>x.LeaseId).OnDelete(DeleteBehavior.Restrict);
  b.Entity<AuditEntry>().ToTable("Audit");
  b.Entity<RentalComment>().ToTable("Comments");b.Entity<RentalComment>().HasIndex(x=>x.CreatedAt);
  b.Entity<AppNotification>().ToTable("Notifications");b.Entity<AppNotification>().HasIndex(x=>new{x.IsRead,x.CreatedAt});
  foreach(var entity in b.Model.GetEntityTypes())foreach(var property in entity.GetProperties())
   if(property.ClrType==typeof(decimal))property.SetPrecision(18);
  foreach(var entity in b.Model.GetEntityTypes())foreach(var property in entity.GetProperties())
   if(property.ClrType==typeof(decimal))property.SetScale(2);
 }
}
