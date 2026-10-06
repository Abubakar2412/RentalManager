using System.ComponentModel.DataAnnotations;
namespace RentalManager;
public interface IEntity { int Id {get;set;} }
public class Property : IEntity {
 [Required,RegularExpression("House|Shop"),StringLength(10)] public string Kind {get;set;}="House";
 [Range(0,100000)] public decimal FloorArea {get;set;}
 [StringLength(150)] public string BusinessType {get;set;}="";
 [Timestamp] public byte[] RowVersion {get;set;}=[];
 public int Id {get;set;}
 [Required,StringLength(120)] public string Name {get;set;}="";
 [Required,StringLength(250)] public string Address {get;set;}="";
 [Range(0,30)] public int Bedrooms {get;set;}=1;
 [Range(0,30)] public int Bathrooms {get;set;}=1;
 [Range(1,1000000000)] public decimal MonthlyRent {get;set;}
 [StringLength(3000)] public string Amenities {get;set;}="";
 public bool Available {get;set;}=true;
}
public class Tenant : IEntity {
 [Timestamp] public byte[] RowVersion {get;set;}=[];
 public int Id {get;set;}
 [Required,StringLength(120)] public string FullName {get;set;}="";
 [Required,StringLength(80)] public string NationalId {get;set;}="";
 [Required,StringLength(40)] public string Phone {get;set;}="";
 [EmailAddress,StringLength(150)] public string? Email {get;set;}
 [Required,StringLength(250)] public string Address {get;set;}="";
 [StringLength(150)] public string Occupation {get;set;}="";
 [StringLength(120)] public string EmergencyName {get;set;}="";
 [StringLength(40)] public string EmergencyPhone {get;set;}="";
 [StringLength(100)] public string BankName {get;set;}="";
 [StringLength(120)] public string AccountName {get;set;}="";
 [StringLength(80)] public string AccountNumber {get;set;}="";
}
public class Lease : IEntity {
 [Timestamp] public byte[] RowVersion {get;set;}=[];
 public int Id {get;set;}
 [Range(1,int.MaxValue)] public int PropertyId {get;set;}
 [Range(1,int.MaxValue)] public int TenantId {get;set;}
 public DateOnly StartDate {get;set;}=DateOnly.FromDateTime(DateTime.Today);
 public DateOnly EndDate {get;set;}=DateOnly.FromDateTime(DateTime.Today.AddYears(1));
 [Range(1,1000000000)] public decimal MonthlyRent {get;set;}
 [Range(0,1000000000)] public decimal Deposit {get;set;}
 [Range(1,28)] public int DueDay {get;set;}=1;
 [Range(1,100)] public int Occupants {get;set;}=1;
 [Range(0,365)] public int NoticeDays {get;set;}=30;
 [RegularExpression("Active|Ended|Cancelled")] public string Status {get;set;}="Active";
 [StringLength(10000)] public string SpecialTerms {get;set;}="";
 [StringLength(120)] public string WitnessName {get;set;}="";
 [StringLength(40)] public string WitnessPhone {get;set;}="";
}
public class Payment : IEntity {
 [Timestamp] public byte[] RowVersion {get;set;}=[];
 public int Id {get;set;}
 [Range(1,int.MaxValue)] public int LeaseId {get;set;}
 public DateOnly PaidOn {get;set;}=DateOnly.FromDateTime(DateTime.Today);
 public DateOnly Period {get;set;}=DateOnly.FromDateTime(DateTime.Today);
 [Range(0.01,1000000000)] public decimal Amount {get;set;}
 [RegularExpression("Rent|Deposit|Other")] public string Kind {get;set;}="Rent";
 [RegularExpression("Bank transfer|Cash|Mobile money")] public string Method {get;set;}="Bank transfer";
 [Required,StringLength(120)] public string Reference {get;set;}="";
 [StringLength(1000)] public string Notes {get;set;}="";
}
public class Landlord : IEntity {
 [Timestamp] public byte[] RowVersion {get;set;}=[];
 public int Id {get;set;}=1;
 [Required,StringLength(150)] public string Name {get;set;}="";
 [Required,StringLength(250)] public string Address {get;set;}="";
 [Required,StringLength(40)] public string Phone {get;set;}="";
 [EmailAddress,StringLength(150)] public string? Email {get;set;}
 [StringLength(80)] public string NationalId {get;set;}="";
 [Required,StringLength(100)] public string BankName {get;set;}="";
 [Required,StringLength(120)] public string AccountName {get;set;}="";
 [Required,StringLength(80)] public string AccountNumber {get;set;}="";
 [Required,StringLength(10)] public string Currency {get;set;}="TZS";
 [StringLength(10000)] public string ContractTerms {get;set;}="The premises may be used only for the purpose stated in this agreement. The tenant must keep the premises clean, report damage promptly, and obtain written consent before subletting or making alterations. The landlord is responsible for agreed structural repairs. Utilities are paid by the tenant unless agreed otherwise. The deposit is refunded after inspection, less documented agreed deductions. Any termination and dispute resolution must comply with applicable law.";
}
public class Admin {
 [StringLength(32)] public string SecurityStamp {get;set;}=Guid.NewGuid().ToString("N");
 public int Id {get;set;}=1;
 [Required,StringLength(80)] public string Username {get;set;}="";
 [Required,StringLength(1000)] public string PasswordHash {get;set;}="";
}
public class ContractSnapshot {
 public int Id {get;set;}
 public int LeaseId {get;set;}
 public DateTimeOffset CreatedAt {get;set;}
 public string Html {get;set;}="";
}
public class AuditEntry {
 public int Id {get;set;}
 public DateTimeOffset At {get;set;}
 [StringLength(80)] public string Actor {get;set;}="";
 [StringLength(30)] public string Action {get;set;}="";
 [StringLength(40)] public string Entity {get;set;}="";
 public int EntityId {get;set;}
}
