using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RentalManager.Data;
namespace RentalManager.Migrations;
[DbContext(typeof(RentalDbContext))]
[Migration("20261009074500_ProfileCommentsNotifications")]
public class ProfileCommentsNotifications:Migration {
 protected override void Up(MigrationBuilder m){
  m.AddColumn<string>(name:"DisplayName",table:"Admins",type:"nvarchar(150)",maxLength:150,nullable:false,defaultValue:"");
  m.AddColumn<string>(name:"Email",table:"Admins",type:"nvarchar(150)",maxLength:150,nullable:true);
  m.AddColumn<string>(name:"Phone",table:"Admins",type:"nvarchar(40)",maxLength:40,nullable:false,defaultValue:"");
  m.CreateTable(name:"Comments",columns:t=>new{
   Id=t.Column<int>(type:"int",nullable:false).Annotation("SqlServer:Identity","1, 1"),
   Subject=t.Column<string>(type:"nvarchar(120)",maxLength:120,nullable:false),
   Message=t.Column<string>(type:"nvarchar(2000)",maxLength:2000,nullable:false),
   Author=t.Column<string>(type:"nvarchar(80)",maxLength:80,nullable:false),
   CreatedAt=t.Column<DateTimeOffset>(type:"datetimeoffset",nullable:false),
   Resolved=t.Column<bool>(type:"bit",nullable:false)
  },constraints:t=>t.PrimaryKey("PK_Comments",x=>x.Id));
  m.CreateTable(name:"Notifications",columns:t=>new{
   Id=t.Column<int>(type:"int",nullable:false).Annotation("SqlServer:Identity","1, 1"),
   Title=t.Column<string>(type:"nvarchar(150)",maxLength:150,nullable:false),
   Message=t.Column<string>(type:"nvarchar(250)",maxLength:250,nullable:false),
   Link=t.Column<string>(type:"nvarchar(120)",maxLength:120,nullable:false),
   CreatedAt=t.Column<DateTimeOffset>(type:"datetimeoffset",nullable:false),
   IsRead=t.Column<bool>(type:"bit",nullable:false)
  },constraints:t=>t.PrimaryKey("PK_Notifications",x=>x.Id));
  m.CreateIndex(name:"IX_Comments_CreatedAt",table:"Comments",column:"CreatedAt");
  m.CreateIndex(name:"IX_Notifications_IsRead_CreatedAt",table:"Notifications",columns:new[]{"IsRead","CreatedAt"});
 }
 protected override void Down(MigrationBuilder m){m.DropTable("Comments");m.DropTable("Notifications");m.DropColumn("DisplayName","Admins");m.DropColumn("Email","Admins");m.DropColumn("Phone","Admins");}
}
