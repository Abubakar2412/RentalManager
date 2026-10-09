using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RentalManager.Data;
namespace RentalManager.Migrations;
[DbContext(typeof(RentalDbContext))]
[Migration("20261009155500_ClientPortal")]
public class ClientPortal:Migration {
 protected override void Up(MigrationBuilder m){
  m.CreateTable(name:"ClientAccounts",columns:t=>new{
   Id=t.Column<int>(type:"int",nullable:false).Annotation("SqlServer:Identity","1, 1"),TenantId=t.Column<int>(type:"int",nullable:false),
   Username=t.Column<string>(type:"nvarchar(80)",maxLength:80,nullable:false),PasswordHash=t.Column<string>(type:"nvarchar(1000)",maxLength:1000,nullable:false),SecurityStamp=t.Column<string>(type:"nvarchar(32)",maxLength:32,nullable:false),
   Enabled=t.Column<bool>(type:"bit",nullable:false),MustChangePassword=t.Column<bool>(type:"bit",nullable:false)
  },constraints:t=>{t.PrimaryKey("PK_ClientAccounts",x=>x.Id);t.ForeignKey("FK_ClientAccounts_Tenants_TenantId",x=>x.TenantId,"Tenants","Id",onDelete:ReferentialAction.Restrict);});
  m.CreateTable(name:"RenewalIntents",columns:t=>new{
   Id=t.Column<int>(type:"int",nullable:false).Annotation("SqlServer:Identity","1, 1"),LeaseId=t.Column<int>(type:"int",nullable:false),TenantId=t.Column<int>(type:"int",nullable:false),ContinueRent=t.Column<bool>(type:"bit",nullable:false),Months=t.Column<int>(type:"int",nullable:false),
   Message=t.Column<string>(type:"nvarchar(2000)",maxLength:2000,nullable:false),SubmittedAt=t.Column<DateTimeOffset>(type:"datetimeoffset",nullable:false),Reviewed=t.Column<bool>(type:"bit",nullable:false),Response=t.Column<string>(type:"nvarchar(1000)",maxLength:1000,nullable:false)
  },constraints:t=>{t.PrimaryKey("PK_RenewalIntents",x=>x.Id);t.ForeignKey("FK_RenewalIntents_Leases_LeaseId",x=>x.LeaseId,"Leases","Id",onDelete:ReferentialAction.Restrict);t.ForeignKey("FK_RenewalIntents_Tenants_TenantId",x=>x.TenantId,"Tenants","Id",onDelete:ReferentialAction.Restrict);});
  m.CreateIndex("IX_ClientAccounts_Username","ClientAccounts","Username",unique:true);m.CreateIndex("IX_ClientAccounts_TenantId","ClientAccounts","TenantId",unique:true);
  m.CreateIndex("IX_RenewalIntents_LeaseId","RenewalIntents","LeaseId",unique:true);m.CreateIndex("IX_RenewalIntents_TenantId","RenewalIntents","TenantId");
 }
 protected override void Down(MigrationBuilder m){m.DropTable("RenewalIntents");m.DropTable("ClientAccounts");}
}
