using System.ComponentModel.DataAnnotations;
namespace RentalManager.Services;
public record ClientIdentity(int AccountId,int TenantId,string Username);
public interface ICurrentClient {Task<ClientIdentity> GetRequiredAsync();}
public record ClientSession(int AccountId,int TenantId,string Username,string Stamp,bool MustChangePassword);
public record ClientAccountInfo(int Id,int TenantId,string TenantName,string Username,bool Enabled,bool MustChangePassword);
public record ClientLease(int Id,string Property,DateOnly Start,DateOnly End,decimal MonthlyRent,string Status,int NoticeDays,int RenewalMonths);
public record ClientContract(int Id,int LeaseId,DateTimeOffset CreatedAt);
public record RenewalInfo(int Id,int LeaseId,string Tenant,string Property,bool ContinueRent,int Months,string Message,DateTimeOffset SubmittedAt,bool Reviewed,string Response);
public record ClientDashboard(string Name,string Currency,IReadOnlyList<ClientLease> Leases,IReadOnlyList<ClientContract> Contracts,IReadOnlyList<RenewalInfo> Requests);
public record ExpiryReminder(int LeaseId,string Property,DateOnly End,int DaysRemaining) {
 public string Message=>DaysRemaining<0?$"Your rental term ended on {End:yyyy-MM-dd}. Contact the landlord.":$"Your rental term ends on {End:yyyy-MM-dd} ({DaysRemaining} day(s) remaining). Tell the landlord whether you plan to continue.";
}
public sealed class ClientProvision {
 [Range(1,int.MaxValue)]public int TenantId{get;set;}
 [Required,StringLength(80,MinimumLength=3)]public string Username{get;set;}="";
 [Required,StringLength(200,MinimumLength=12)]public string TemporaryPassword{get;set;}="";
}
public sealed class RenewalDraft {
 [Range(1,int.MaxValue)]public int LeaseId{get;set;}
 public bool ContinueRent{get;set;}=true;
 [Range(1,12)]public int Months{get;set;}=6;
 [StringLength(2000)]public string Message{get;set;}="";
}
public interface IClientAccountService {
 Task<ClientSession?> VerifyAsync(string username,string password);
 Task<bool> ValidateAsync(int id,int tenantId,string username,string? stamp,CancellationToken token=default);
 Task<bool> ChangePasswordAsync(int id,string current,string password);
 Task<IReadOnlyList<ClientAccountInfo>> ListAsync();
 Task CreateAsync(ClientProvision input,string actor);
 Task ResetAsync(int id,string temporaryPassword,string actor);
 Task EnableAsync(int id,bool enabled,string actor);
}
public interface IClientPortalRepository {
 Task<ClientDashboard> LoadAsync(int tenantId);
 Task<string?> ContractAsync(int id,int tenantId);
 Task SubmitAsync(RenewalDraft draft,int tenantId,string actor,DateOnly today);
 Task<IReadOnlyList<RenewalInfo>> RequestsAsync();
 Task ReviewAsync(int id,string response,string actor);
}
public sealed class ClientPortalService(IClientPortalRepository repository,ICurrentClient client,TimeProvider clock) {
 public DateOnly Today=>DateOnly.FromDateTime(clock.GetUtcNow().ToOffset(TimeSpan.FromHours(3)).DateTime);
 public async Task<ClientDashboard> LoadAsync()=>await repository.LoadAsync((await client.GetRequiredAsync()).TenantId);
 public async Task<string?> ContractAsync(int id)=>await repository.ContractAsync(id,(await client.GetRequiredAsync()).TenantId);
 public async Task SubmitAsync(RenewalDraft draft){Validate(draft);var identity=await client.GetRequiredAsync();await repository.SubmitAsync(draft,identity.TenantId,identity.Username,Today);}
 public IReadOnlyList<ExpiryReminder> Reminders(ClientDashboard data)=>data.Leases.Where(x=>x.Status=="Active"&&x.End.DayNumber-Today.DayNumber<=x.NoticeDays).OrderBy(x=>x.End).Select(x=>new ExpiryReminder(x.Id,x.Property,x.End,x.End.DayNumber-Today.DayNumber)).ToList();
 internal static void Validate(object value){var results=new List<ValidationResult>();if(!Validator.TryValidateObject(value,new ValidationContext(value),results,true))throw new InvalidOperationException(string.Join(" ",results.Select(x=>x.ErrorMessage)));}
}
public sealed class ClientAdminService(ICurrentUser user,IClientAccountService accounts,IClientPortalRepository repository) {
 public async Task<IReadOnlyList<ClientAccountInfo>> AccountsAsync(){await user.GetRequiredNameAsync();return await accounts.ListAsync();}
 public async Task CreateAsync(ClientProvision input){input.Username=input.Username.Trim();ClientPortalService.Validate(input);await accounts.CreateAsync(input,await user.GetRequiredNameAsync());input.TemporaryPassword="";}
 public async Task ResetAsync(int id,string password){if(id<1||password.Length<12||password.Length>200)throw new InvalidOperationException("Select an account and a 12–200 character temporary password.");await accounts.ResetAsync(id,password,await user.GetRequiredNameAsync());}
 public async Task EnableAsync(int id,bool enabled)=>await accounts.EnableAsync(id,enabled,await user.GetRequiredNameAsync());
 public async Task<IReadOnlyList<RenewalInfo>> RequestsAsync(){await user.GetRequiredNameAsync();return await repository.RequestsAsync();}
 public async Task ReviewAsync(int id,string response){if(response.Length>1000)throw new InvalidOperationException("Response must be at most 1,000 characters.");await repository.ReviewAsync(id,response.Trim(),await user.GetRequiredNameAsync());}
}
