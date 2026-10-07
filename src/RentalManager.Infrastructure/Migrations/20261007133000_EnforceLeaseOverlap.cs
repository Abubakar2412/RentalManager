using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RentalManager.Data;
namespace RentalManager.Migrations;
[DbContext(typeof(RentalDbContext))]
[Migration("20261007133000_EnforceLeaseOverlap")]
public class EnforceLeaseOverlap : Migration {
 protected override void Up(MigrationBuilder m) => m.Sql("""
CREATE OR ALTER TRIGGER [TR_Leases_NoOverlap] ON [Leases] AFTER INSERT,UPDATE AS
BEGIN
 SET NOCOUNT ON;
 IF EXISTS (
  SELECT 1 FROM inserted i JOIN [Leases] l WITH (UPDLOCK,HOLDLOCK)
  ON l.PropertyId=i.PropertyId AND l.Id<>i.Id
  AND l.Status='Active' AND i.Status='Active'
  AND l.StartDate<=i.EndDate AND l.EndDate>=i.StartDate
 )
 BEGIN
  THROW 51001, 'Overlapping active rental lease for this house or shop.', 1;
 END
END
""");
 protected override void Down(MigrationBuilder m) => m.Sql("DROP TRIGGER IF EXISTS [TR_Leases_NoOverlap]");
}
