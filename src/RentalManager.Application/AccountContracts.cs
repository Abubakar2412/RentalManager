namespace RentalManager.Services;
public record AdminSession(string Username,string SecurityStamp);
public interface IAdminAccountService {
 Task<bool> ExistsAsync();
 Task<bool> CreateAsync(string username,string password);
 Task<AdminSession?> VerifyAsync(string username,string password);
 Task<bool> ChangePasswordAsync(string username,string currentPassword,string newPassword);
 Task<bool> ValidateAsync(string username,string? stamp,CancellationToken cancellationToken=default);
}
