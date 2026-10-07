namespace RentalManager.Services;
public enum RentalLoadScope { Core, Contracts, Audit, All }
public interface ICurrentUser { Task<string> GetRequiredNameAsync(); }
public interface IContractRenderer { string Render(Lease lease,Property property,Tenant tenant,Landlord landlord); }
public interface IRentalRepository {
 Task<RentalData> LoadAsync(string actor,RentalLoadScope scope);
 Task SaveAsync(IEntity entity,string actor);
 Task SaveRentInstalmentAsync(Payment payment,int months,string actor);
 Task DeleteAsync(IEntity entity,string actor);
 Task<int> GenerateContractAsync(int id,string actor);
}
public sealed class RentalPersistenceException(string message,Exception inner):Exception(message,inner);
