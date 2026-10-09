using System.Globalization;
namespace RentalManager.Services;
public static class CsvExport {
 public static string Cell(object? value){var text=value?.ToString()??"";if(text.Length>0&&"=+-@\t\r".Contains(text[0]))text="'"+text;return "\""+text.Replace("\"","\"\"")+"\"";}
 public static string Records(IEnumerable<IEntity> items){var rows=items.ToList();if(rows.Count==0)return "";var props=rows[0].GetType().GetProperties().Where(x=>x.Name!="RowVersion").ToArray();return "\uFEFF"+string.Join(",",props.Select(p=>Cell(p.Name)))+"\r\n"+string.Join("\r\n",rows.Select(x=>string.Join(",",props.Select(p=>Cell(p.GetValue(x))))));}
 public static string Statement(Lease lease,RentalData data){
  var now=DateOnly.FromDateTime(DateTime.Today);var end=lease.EndDate<now?lease.EndDate:now;
  var lines=new List<string>{"Lease,Property,Tenant,Month,Currency,Rent,DueDate,Paid,Balance,Due"};
  var cursor=new DateOnly(lease.StartDate.Year,lease.StartDate.Month,1);var last=new DateOnly(end.Year,end.Month,1);
  while(cursor<=last&&lease.StartDate<=end){
   var paid=data.Payments.Where(p=>p.LeaseId==lease.Id&&p.Kind=="Rent"&&p.Period.Year==cursor.Year&&p.Period.Month==cursor.Month).Sum(p=>p.Amount);
   var due=new DateOnly(cursor.Year,cursor.Month,lease.DueDay);
   lines.Add(string.Join(",",new object?[]{lease.Id,data.PropertyName(lease.PropertyId),data.TenantName(lease.TenantId),cursor.ToString("yyyy-MM"),data.Landlord.Currency,lease.MonthlyRent.ToString(CultureInfo.InvariantCulture),due.ToString("yyyy-MM-dd"),paid.ToString(CultureInfo.InvariantCulture),(lease.MonthlyRent-paid).ToString(CultureInfo.InvariantCulture),due<=now}.Select(Cell)));
   cursor=cursor.AddMonths(1);
  }
  return "\uFEFF"+string.Join("\r\n",lines);
 }
}
