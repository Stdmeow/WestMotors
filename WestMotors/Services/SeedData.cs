using Microsoft.AspNetCore.Identity;
using WestMotorsApp.Models; // Убедитесь, что namespace соответствует вашему проекту

namespace WestMotorsApp.Services
{
    public static class SeedData
    {
        public static async Task Initialize(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            // Создаем роли, если их нет
            string[] roleNames = { "Администратор", "Менеджер", "Клиент" };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Создаем пользователя-администратора, если его нет
            string adminEmail = "admin@westmotors.com";
            string adminPassword = "AdminPassword123!";
            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FullName = "Главный Администратор",
                    Position = "Администратор",
                    ContactInfo = "+1234567890"
                };
                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Администратор");
                }
            }
        }
    }
}