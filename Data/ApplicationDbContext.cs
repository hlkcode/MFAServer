
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;


namespace MFAServer.Data;

public class ApplicationDbContext : IdentityDbContext<User, Role, long, IdentityUserClaim<long>, UserRole, IdentityUserLogin<long>, IdentityRoleClaim<long>, IdentityUserToken<long>>
{

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
  
    }


    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<UserRole>(userRole =>
        {


            userRole.HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId)
                .IsRequired();

            userRole.HasOne(ur => ur.User)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.UserId);
        });

    }




}