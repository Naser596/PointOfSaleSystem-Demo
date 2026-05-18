using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebApplication3.Models;

namespace WebApplication3.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
            {
                var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

                // Seed Roles
                string[] roleNames = { "SuperAdmin", "Admin", "Manager", "Accountant", "Warehouse", "HR", "Cashier" };
                foreach (var roleName in roleNames)
                {
                    if (!await roleManager.RoleExistsAsync(roleName))
                    {
                        await roleManager.CreateAsync(new IdentityRole(roleName));
                    }
                }

                // Seed Super Admin User
                var adminEmail = "nasermustafi@gmail.com";
                var adminUser = await userManager.FindByEmailAsync(adminEmail);
                if (adminUser == null)
                {
                    adminUser = new ApplicationUser
                    {
                        UserName = adminEmail,
                        Email = adminEmail,
                        EmailConfirmed = true
                    };
                    await userManager.CreateAsync(adminUser, "Admin123");
                }
                if (!await userManager.IsInRoleAsync(adminUser, "SuperAdmin"))
                {
                    await userManager.AddToRoleAsync(adminUser, "SuperAdmin");
                }
                if (await userManager.IsInRoleAsync(adminUser, "Admin"))
                {
                    await userManager.RemoveFromRoleAsync(adminUser, "Admin");
                }

                // Do not seed demo products, categories, sales, or company users.
                // In a multi-company platform every new company must start with empty business data.
            }
        }
    }
}
