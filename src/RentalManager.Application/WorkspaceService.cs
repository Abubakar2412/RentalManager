using System.ComponentModel.DataAnnotations;
namespace RentalManager.Services;
public sealed class ProfileEdit {
 [Required,StringLength(150)]public string DisplayName{get;set;}="";
 [EmailAddress,StringLength(150)]public string? Email{get;set;}
 [StringLength(40)]public string Phone{get;set;}="";
}
public record UserProfile(string Username,string DisplayName,string? Email,string Phone);
public sealed class CommentDraft {
 [Required,StringLength(120)]public string Subject{get;set;}="";
 [Required,StringLength(2000)]public string Message{get;set;}="";
}
public record CommentFilter(string Search="",string Status="all",int Page=1) {
 public void Validate(){if(Page<1||Page>100000||Search.Length>120||Status is not ("all" or "open" or "resolved"))throw new InvalidOperationException("Invalid comment filters.");}
}
public record CommentPage(IReadOnlyList<RentalComment> Items,int Total,int Page);
public record NotificationPage(IReadOnlyList<AppNotification> Items,int Total,int Unread,int Page);
public record WorkspaceSummary(UserProfile Profile,int Unread);
public interface IWorkspaceRepository {
 Task<WorkspaceSummary> SummaryAsync(string actor);
 Task SaveProfileAsync(ProfileEdit profile,string actor);
 Task<CommentPage> CommentsAsync(CommentFilter filter,string actor);
 Task AddCommentAsync(CommentDraft draft,string actor);
 Task ResolveCommentAsync(int id,bool resolved,string actor);
 Task<NotificationPage> NotificationsAsync(bool unreadOnly,int page,string actor);
 Task MarkReadAsync(int? id,string actor);
}
public sealed class WorkspaceService(IWorkspaceRepository repository,ICurrentUser user) {
 public event Action? Changed;
 public Task<WorkspaceSummary> SummaryAsync()=>WithActor(repository.SummaryAsync);
 public async Task SaveProfileAsync(ProfileEdit profile){profile.DisplayName=profile.DisplayName.Trim();profile.Email=string.IsNullOrWhiteSpace(profile.Email)?null:profile.Email.Trim();profile.Phone=profile.Phone.Trim();Validate(profile);await repository.SaveProfileAsync(profile,await user.GetRequiredNameAsync());Changed?.Invoke();}
 public async Task<CommentPage> CommentsAsync(CommentFilter filter){filter.Validate();return await repository.CommentsAsync(filter,await user.GetRequiredNameAsync());}
 public async Task AddCommentAsync(CommentDraft draft){draft.Subject=draft.Subject.Trim();draft.Message=draft.Message.Trim();Validate(draft);await repository.AddCommentAsync(draft,await user.GetRequiredNameAsync());Changed?.Invoke();}
 public async Task ResolveCommentAsync(int id,bool resolved){if(id<1)throw new InvalidOperationException("Select a valid comment.");await repository.ResolveCommentAsync(id,resolved,await user.GetRequiredNameAsync());Changed?.Invoke();}
 public async Task<NotificationPage> NotificationsAsync(bool unreadOnly,int page){if(page<1||page>100000)throw new InvalidOperationException("Invalid notification page.");return await repository.NotificationsAsync(unreadOnly,page,await user.GetRequiredNameAsync());}
 public async Task MarkReadAsync(int? id){if(id is <1)throw new InvalidOperationException("Select a valid notification.");await repository.MarkReadAsync(id,await user.GetRequiredNameAsync());Changed?.Invoke();}
 async Task<T> WithActor<T>(Func<string,Task<T>> work)=>await work(await user.GetRequiredNameAsync());
 static void Validate(object value){var results=new List<ValidationResult>();if(!Validator.TryValidateObject(value,new ValidationContext(value),results,true))throw new InvalidOperationException(string.Join(" ",results.Select(x=>x.ErrorMessage)));}
}
