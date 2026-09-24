using CVPlatform.Domain.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace CVPlatform.Infrastructure.Identity;

public static class AdminSeeder
{
    public static async Task SeedAsync(UserManager<ApplicationUser> userManager, IConfiguration configuration)
    {
        var existingAdmins = await userManager.GetUsersInRoleAsync(Roles.Administrator);
        if (existingAdmins.Count > 0)
            return;

        var email = configuration["AdminSeed:Email"];
        var password = configuration["AdminSeed:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return;

        var user = await userManager.FindByEmailAsync(email);

        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = configuration["AdminSeed:FirstName"] ?? "System",
                LastName = configuration["AdminSeed:LastName"] ?? "Administrator"
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
                return;
        }

        await userManager.AddToRoleAsync(user, Roles.Administrator);
    }
}