using RentalManager;
using RentalManager.Services;
using Xunit;
public class ApplicationBoundaryTests {
 sealed class User(bool allowed):ICurrentUser {
  public Task<string> GetRequiredNameAsync()=>allowed?Task.FromResult("administrator"):throw new UnauthorizedAccessException();
 }
 sealed class Repository:IRentalRepository {
  public string? Actor;public RentalLoadScope? Scope;
  public Task<RentalData> LoadAsync(string actor,RentalLoadScope scope){Actor=actor;Scope=scope;return Task.FromResult(new RentalData());}
  public Task SaveAsync(IEntity entity,string actor){Actor=actor;return Task.CompletedTask;}
  public Task DeleteAsync(IEntity entity,string actor){Actor=actor;return Task.CompletedTask;}
  public Task SaveRentInstalmentAsync(Payment payment,int months,string actor){Actor=actor;return Task.CompletedTask;}
  public Task<int> GenerateContractAsync(int id,string actor){Actor=actor;return Task.FromResult(id);}
 }
 [Fact] public async Task UnauthenticatedRequestCannotReachPersistence(){var repository=new Repository();var service=new RentalService(repository,new User(false));await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>service.LoadAsync());Assert.Null(repository.Actor);}
 [Fact] public async Task ActorAndLoadScopePassThroughBoundary(){var repository=new Repository();var service=new RentalService(repository,new User(true));await service.LoadAsync(RentalLoadScope.Audit);Assert.Equal("administrator",repository.Actor);Assert.Equal(RentalLoadScope.Audit,repository.Scope);}
}
