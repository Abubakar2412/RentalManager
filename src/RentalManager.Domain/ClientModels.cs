using System.ComponentModel.DataAnnotations;
namespace RentalManager;
public class ClientAccount {
 public int Id{get;set;}
 public int TenantId{get;set;}
 [Required,StringLength(80)]public string Username{get;set;}="";
 [Required,StringLength(1000)]public string PasswordHash{get;set;}="";
 [Required,StringLength(32)]public string SecurityStamp{get;set;}=Guid.NewGuid().ToString("N");
 public bool Enabled{get;set;}=true;
 public bool MustChangePassword{get;set;}=true;
}
public class RenewalIntent {
 public int Id{get;set;}
 public int LeaseId{get;set;}
 public int TenantId{get;set;}
 public bool ContinueRent{get;set;}
 public int Months{get;set;}=6;
 [StringLength(2000)]public string Message{get;set;}="";
 public DateTimeOffset SubmittedAt{get;set;}
 public bool Reviewed{get;set;}
 [StringLength(1000)]public string Response{get;set;}="";
}
