using RentalManager;
using RentalManager.Services;
using Xunit;
public class WorkspaceBoundaryTests {
 sealed class User(bool allowed):ICurrentUser{public Task<string> GetRequiredNameAsync()=>allowed?Task.FromResult("owner"):throw new UnauthorizedAccessException();}
 sealed class Repository:IWorkspaceRepository {
  public int Calls;
  public Task<WorkspaceSummary> SummaryAsync(string actor){Calls++;return Task.FromResult(new WorkspaceSummary(new(actor,"",null,""),0));}
  public Task SaveProfileAsync(ProfileEdit profile,string actor){Calls++;return Task.CompletedTask;}
  public Task<CommentPage> CommentsAsync(CommentFilter filter,string actor){Calls++;return Task.FromResult(new CommentPage([],0,1));}
  public Task AddCommentAsync(CommentDraft draft,string actor){Calls++;return Task.CompletedTask;}
  public Task ResolveCommentAsync(int id,bool resolved,string actor){Calls++;return Task.CompletedTask;}
  public Task<NotificationPage> NotificationsAsync(bool unreadOnly,int page,string actor){Calls++;return Task.FromResult(new NotificationPage([],0,0,1));}
  public Task MarkReadAsync(int? id,string actor){Calls++;return Task.CompletedTask;}
 }
 [Fact]public async Task UnauthenticatedWorkspaceRequestsNeverReachStorage(){var repository=new Repository();var service=new WorkspaceService(repository,new User(false));await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>service.SummaryAsync());await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>service.SaveProfileAsync(new(){DisplayName="Owner"}));await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>service.AddCommentAsync(new(){Subject="Maintenance",Message="Follow up"}));await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>service.CommentsAsync(new()));await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>service.ResolveCommentAsync(1,true));await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>service.NotificationsAsync(false,1));await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>service.MarkReadAsync(null));Assert.Equal(0,repository.Calls);}
 [Fact]public async Task InvalidProfileAndCommentCannotProduceSideEffects(){var repository=new Repository();var service=new WorkspaceService(repository,new User(true));await Assert.ThrowsAsync<InvalidOperationException>(()=>service.SaveProfileAsync(new(){DisplayName=" ",Email="not-an-email"}));await Assert.ThrowsAsync<InvalidOperationException>(()=>service.AddCommentAsync(new(){Subject=" ",Message="text"}));await Assert.ThrowsAsync<InvalidOperationException>(()=>service.AddCommentAsync(new(){Subject="Subject",Message=new string('x',2001)}));await Assert.ThrowsAsync<InvalidOperationException>(()=>service.CommentsAsync(new(Status:"invalid")));await Assert.ThrowsAsync<InvalidOperationException>(()=>service.NotificationsAsync(false,0));Assert.Equal(0,repository.Calls);}
}
