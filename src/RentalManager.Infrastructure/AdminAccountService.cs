using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RentalManager.Data;
namespace RentalManager.Services;
public sealed class AdminAccountService(IDbContextFactory<RentalDbContext> factory):IAdminAccountService {
 static void Audit(RentalDbContext db,string actor,string action){db.Audit.Add(new(){At=DateTimeOffset.UtcNow,Actor=actor,Action=action,Entity="Admin",EntityId=1});db.Notifications.Add(new(){Title=action switch{"SetupAdmin"=>"Administrator account created","SignIn"=>"Administrator signed in","SignOut"=>"Administrator signed out","RenameAdmin"=>"Administrator username changed","ChangePassword"=>"Administrator password changed",_=>"Administrator activity"},Message=$"{actor}: account activity recorded.",Link="/settings?tab=activity",CreatedAt=DateTimeOffset.UtcNow});}
 public async Task RecordSignOutAsync(string username){await using var db=await factory.CreateDbContextAsync();Audit(db,username,"SignOut");await db.SaveChangesAsync();}
 public async Task<bool> RenameAsync(string username,string password,string newUsername){
  newUsername=newUsername.Trim();if(newUsername.Length<3||newUsername.Length>80)throw new InvalidOperationException("Username must be 3–80 characters.");
  await using var db=await factory.CreateDbContextAsync();await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
  var admin=await db.Admins.SingleOrDefaultAsync(x=>x.Username==username);if(admin is null||new PasswordHasher<string>().VerifyHashedPassword(admin.Username,admin.PasswordHash,password)==PasswordVerificationResult.Failed)return false;
  admin.Username=newUsername;admin.PasswordHash=new PasswordHasher<string>().HashPassword(newUsername,password);admin.SecurityStamp=Guid.NewGuid().ToString("N");Audit(db,username,"RenameAdmin");await db.SaveChangesAsync();await tx.CommitAsync();return true;
 }
 public async Task<bool> ExistsAsync(){await using var db=await factory.CreateDbContextAsync();return await db.Admins.AnyAsync();}
 public async Task<bool> CreateAsync(string username,string password){
  username=username.Trim();if(username.Length<3||username.Length>80||password.Length<12||password.Length>200)throw new InvalidOperationException("Enter a valid username and a password of at least 12 characters.");
  await using var db=await factory.CreateDbContextAsync();if(await db.Admins.AnyAsync())return false;
  db.Admins.Add(new(){Id=1,Username=username,PasswordHash=new PasswordHasher<string>().HashPassword(username,password)});
  Audit(db,username,"SetupAdmin");try{await db.SaveChangesAsync();return true;}catch(DbUpdateException){return false;}
 }
 public async Task<AdminSession?> VerifyAsync(string username,string password){await using var db=await factory.CreateDbContextAsync();var admin=await db.Admins.AsNoTracking().SingleOrDefaultAsync();if(admin is null)return null;
  if(username.Trim()!=admin.Username||new PasswordHasher<string>().VerifyHashedPassword(admin.Username,admin.PasswordHash,password)==PasswordVerificationResult.Failed)return null;
  Audit(db,admin.Username,"SignIn");await db.SaveChangesAsync();return new(admin.Username,admin.SecurityStamp);
 }
 public async Task<bool> ChangePasswordAsync(string username,string currentPassword,string newPassword){
  if(newPassword.Length<12||newPassword.Length>200)throw new InvalidOperationException("Password must be 12–200 characters.");
  await using var db=await factory.CreateDbContextAsync();var admin=await db.Admins.SingleOrDefaultAsync(x=>x.Username==username);if(admin is null)return false;
  var hasher=new PasswordHasher<string>();if(hasher.VerifyHashedPassword(admin.Username,admin.PasswordHash,currentPassword)==PasswordVerificationResult.Failed)return false;
  admin.PasswordHash=hasher.HashPassword(admin.Username,newPassword);admin.SecurityStamp=Guid.NewGuid().ToString("N");Audit(db,username,"ChangePassword");await db.SaveChangesAsync();return true;
 }
 public async Task<bool> ValidateAsync(string username,string? stamp,CancellationToken cancellationToken=default){await using var db=await factory.CreateDbContextAsync(cancellationToken);return await db.Admins.AsNoTracking().AnyAsync(x=>x.Id==1&&x.Username==username&&x.SecurityStamp==stamp,cancellationToken);}
}
