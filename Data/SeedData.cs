using HlkHelpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MFAServer.Data;

public class SeedData
{

    private readonly static List<string> roles = [

                Privileges.CanViewDashboard,
                Privileges.CanViewSettings,
                Privileges.CanViewReports,
                Privileges.CanViewRoles,
                Privileges.CanCreateRoles,
                Privileges.CanUpdateRoles,
                Privileges.CanDeleteRoles,
                Privileges.CanViewUsers,
                Privileges.CanCreateUsers,
                Privileges.CanUpdateUsers,
                Privileges.CanDeleteUsers,
                Privileges.CanCreateSettings,
                Privileges.CanUpdateSettings,
                Privileges.CanDeleteSettings

            ];

    public static void Initialize(IServiceProvider serviceProvider)
    {

        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<User>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<Role>>();
        var config = serviceProvider.GetRequiredService<IConfiguration>();
        var logger = serviceProvider.GetRequiredService<ILogger<SeedData>>();



        if (!context.Users.Any())
        {

            #region Roles
            SetRole(context, Constants.SuperAdminRole, true);
            SetRole(context, Constants.AdminRole, true);
            SetRole(context, Constants.ManagerRole, true);

            SetRole(context, Constants.ClientRole);
            SetRole(context, Constants.UserRole);

            #endregion


            #region Admin
            var adminUser = new User
            {
                UserName = Constants.SuperAdmin,
                Email = Constants.SuperAdminEmail,
                PhoneNumber = Constants.SuperAdminPhoneNumber,
                CreatedAt = DateTime.UtcNow,
                FullName = Constants.SuperAdmin,
                IsActive = true,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true
            };
            var existingAdmin = context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Email == adminUser.Email).Result;

            // create admin user if not exist
            if (existingAdmin is null)
            {
                var res = userManager.CreateAsync(adminUser, "AdminP@ssw0rd").GetAwaiter().GetResult();
                if (res.Succeeded)
                {
                    adminUser = context.Users.IgnoreQueryFilters().FirstAsync(x => x.Email == adminUser.Email).Result;
                    var adminRole = context.Roles.IgnoreQueryFilters().FirstAsync(r => r.Name == Constants.SuperAdminRole).Result;

                    // role is assigned manually
                   var roleRes = userManager.AddToRoleAsync(adminUser, Constants.SuperAdminRole).Result;
                   // context.UserRoles.Add(new() { RoleId = adminRole.Id, UserId = adminUser.Id});
                    context.SaveChanges();
                }
            }
            #endregion


            context.SaveChanges();

        }

    }

    private static void SetRole(ApplicationDbContext context, string roleName, bool isAdmin = false)
    {
        var adminRole = new Role() { Name = roleName };

        if (!context.Roles.Any(x => x.Name == roleName))
        {
            context.Roles.Add(adminRole);
            context.SaveChanges();

            if (isAdmin)
            {
                var list = roles.Distinct().Select(rc => new RoleClaim() { RoleId = adminRole.Id, ClaimType = Constants.Privilege, ClaimValue = rc });
                context.RoleClaims.AddRange(list);
                context.SaveChanges();
            }

        }

    }
}
