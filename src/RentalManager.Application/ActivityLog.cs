namespace RentalManager.Services;
public record ActivityLogFilter(string Search="",string Action="",DateOnly? From=null,DateOnly? To=null,int Page=1,int PageSize=25) {
 public void Validate(){if(Page<1||Page>100000||PageSize<1||PageSize>100)throw new InvalidOperationException("Invalid activity page.");if(From>To)throw new InvalidOperationException("Start date must precede end date.");if(Search.Length>80||Action.Length>30)throw new InvalidOperationException("Search text is too long.");}
}
public record ActivityLogPage(IReadOnlyList<AuditEntry> Items,int Total,int Page,int PageSize);
