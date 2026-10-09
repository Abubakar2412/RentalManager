using System.ComponentModel.DataAnnotations;
namespace RentalManager;
public class RentalComment {
 public int Id {get;set;}
 [Required,StringLength(120)]public string Subject{get;set;}="";
 [Required,StringLength(2000)]public string Message{get;set;}="";
 [Required,StringLength(80)]public string Author{get;set;}="";
 public DateTimeOffset CreatedAt{get;set;}
 public bool Resolved{get;set;}
}
public class AppNotification {
 public int Id{get;set;}
 [Required,StringLength(150)]public string Title{get;set;}="";
 [Required,StringLength(250)]public string Message{get;set;}="";
 [Required,StringLength(120)]public string Link{get;set;}="";
 public DateTimeOffset CreatedAt{get;set;}
 public bool IsRead{get;set;}
}
