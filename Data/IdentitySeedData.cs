using SportsStore.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace SportsStore.Data
{
    public static class IdentitySeedData
    {
        public const string AdminRole = "Admin";
        public const string AdminUser = "admin";
        public const string AdminPassword = "Admin@123";
        public const string AdminEmail = "admin@sportsstore.local";

        public static async Task EnsurePopulated(IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
            if (context.Database.GetPendingMigrations().Any())
            {
                context.Database.Migrate();
            }

            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

            if (!await roleManager.RoleExistsAsync(AdminRole))
            {
                await roleManager.CreateAsync(new IdentityRole(AdminRole));
            }

            IdentityUser? user = await userManager.FindByNameAsync(AdminUser);
            if (user == null)
            {
                user = new IdentityUser
                {
                    UserName = AdminUser,
                    Email = AdminEmail,
                    EmailConfirmed = true
                };
                IdentityResult result = await userManager.CreateAsync(user, AdminPassword);
                if (!result.Succeeded)
                {
                    throw new Exception("Không tạo được user admin: "
                        + string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }

            if (!await userManager.IsInRoleAsync(user, AdminRole))
            {
                await userManager.AddToRoleAsync(user, AdminRole);
            }
        }
    }
}
