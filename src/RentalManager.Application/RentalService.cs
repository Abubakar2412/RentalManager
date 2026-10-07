namespace RentalManager.Services;
public sealed class RentalService(IRentalRepository repository,ICurrentUser user) {
 public async Task<RentalData> LoadAsync(RentalLoadScope scope=RentalLoadScope.All)=>await repository.LoadAsync(await user.GetRequiredNameAsync(),scope);
 public async Task SaveAsync(IEntity entity)=>await repository.SaveAsync(entity,await user.GetRequiredNameAsync());
 public async Task SaveRentInstalmentAsync(Payment payment,int months)=>await repository.SaveRentInstalmentAsync(payment,months,await user.GetRequiredNameAsync());
 public async Task DeleteAsync(IEntity entity)=>await repository.DeleteAsync(entity,await user.GetRequiredNameAsync());
 public async Task<int> GenerateContractAsync(int id)=>await repository.GenerateContractAsync(id,await user.GetRequiredNameAsync());
 public static string Error(Exception e)=>e is RentalPersistenceException or InvalidOperationException or UnauthorizedAccessException ? e.Message : "The operation failed. Check the server log or SQL Server connection.";
}
